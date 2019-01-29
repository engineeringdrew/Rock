﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Financial
{
    /// <summary>
    /// This class handles charging and then storing a payment. Payments must be made through a gateway
    /// supporting automated charging. Payments must be made for an existing person with a saved account.
    /// Use a new instance of this class for every payment.
    /// </summary>
    public class AutomatedPaymentProcessor
    {
        // Constructor params
        private RockContext _rockContext;
        private AutomatedPaymentArgs _automatedPaymentArgs;
        private int? _currentPersonAliasId;
        private bool _ignoreRepeatChargeProtection;
        private bool _ignoreScheduleAdherenceProtection;

        // Declared services
        private PersonAliasService _personAliasService;
        private FinancialGatewayService _financialGatewayService;
        private FinancialAccountService _financialAccountService;
        private FinancialPersonSavedAccountService _financialPersonSavedAccountService;
        private FinancialBatchService _financialBatchService;
        private FinancialTransactionService _financialTransactionService;
        private FinancialScheduledTransactionService _financialScheduledTransactionService;

        // Loaded entities
        private Person _authorizedPerson;
        private FinancialGateway _financialGateway;
        private GatewayComponent _automatedGatewayComponent;
        private Dictionary<int, FinancialAccount> _financialAccounts;
        private FinancialPersonSavedAccount _financialPersonSavedAccount;
        private ReferencePaymentInfo _referencePaymentInfo;
        private DefinedValueCache _transactionType;
        private DefinedValueCache _financialSource;
        private FinancialScheduledTransaction _financialScheduledTransaction;
        private int? _currentNumberOfPaymentsForSchedule;

        // Results
        private FinancialTransaction _financialTransaction;

        /// <summary>
        /// Create a new payment processor to handle a single automated payment.
        /// </summary>
        /// <param name="currentPersonAliasId">The current user's person alias ID. Possibly the REST user.</param>
        /// <param name="automatedPaymentArgs">The arguments describing how toi charge the payment and store the resulting transaction</param>
        /// <param name="rockContext">The context to use for loading and saving entities</param>
        /// <param name="ignoreRepeatChargeProtection">If true, the payment will be charged even if there is a similar transaction for the same person within a short time period.</param>
        /// <param name="ignoreScheduleAdherenceProtection">If true and a schedule is indicated in the args, the payment will be charged even if the schedule has already been processed accoring to it's frequency.</param>
        public AutomatedPaymentProcessor( int? currentPersonAliasId, AutomatedPaymentArgs automatedPaymentArgs, RockContext rockContext, bool ignoreRepeatChargeProtection, bool ignoreScheduleAdherenceProtection )
        {
            _rockContext = rockContext;
            _automatedPaymentArgs = automatedPaymentArgs;
            _currentPersonAliasId = currentPersonAliasId;
            _ignoreRepeatChargeProtection = ignoreRepeatChargeProtection;
            _ignoreScheduleAdherenceProtection = ignoreScheduleAdherenceProtection;

            _personAliasService = new PersonAliasService( rockContext );
            _financialGatewayService = new FinancialGatewayService( rockContext );
            _financialAccountService = new FinancialAccountService( _rockContext );
            _financialPersonSavedAccountService = new FinancialPersonSavedAccountService( rockContext );
            _financialBatchService = new FinancialBatchService( rockContext );
            _financialTransactionService = new FinancialTransactionService( _rockContext );
            _financialScheduledTransactionService = new FinancialScheduledTransactionService( _rockContext );

            _financialTransaction = null;
        }

        /// <summary>
        /// Validates that the args do not seem to be a repeat charge on the same person in a short timeframe.
        /// Entities are loaded from supplied IDs where applicable to ensure existance and a valid state.
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if charge does not seem repeated. Otherwise a message will be set indicating the problem.</param>
        /// <returns>True if the charge is a repeat. False otherwise.</returns>
        public bool IsRepeatCharge( out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( _ignoreRepeatChargeProtection )
            {
                return false;
            }

            LoadEntities();

            var personAliasIds = _personAliasService.Queryable()
                .AsNoTracking()
                .Where( a => a.Person.GivingId == _authorizedPerson.GivingId )
                .Select( a => a.Id )
                .ToList();

            // Check to see if a transaction exists for the person aliases within the last 5 minutes. This should help eliminate accidental repeat charges.
            var minDateTime = RockDateTime.Now.AddMinutes( -5 );
            var repeatTransaction = _financialTransactionService.Queryable()
                .AsNoTracking()
                .Where( t => t.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains( t.AuthorizedPersonAliasId.Value ) )
                .Where( t => t.TransactionDateTime >= minDateTime )
                .FirstOrDefault();

            if ( repeatTransaction != null )
            {
                errorMessage = string.Format( "Found a likely repeat charge. Check transaction id: {0}. Use IgnoreRepeatChargeProtection option to disable this protection.", repeatTransaction.Id );
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates that the frequency of the scheduled transaction appears to be adhered to 
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if charge appears to follow the schedule frequency. Otherwise a message will be set indicating the problem.</param>
        /// <returns>True if the charge appears to follow the schedule. False otherwise</returns>
        public bool IsAccordingToSchedule( out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( _ignoreScheduleAdherenceProtection || !_automatedPaymentArgs.ScheduledTransactionId.HasValue )
            {
                return true;
            }

            LoadEntities();
            var instructionsToIgnore = "Use IgnoreScheduleAdherenceProtection option to disable this protection.";

            if ( _financialScheduledTransaction == null )
            {
                errorMessage = string.Format( "The scheduled transaction did not resolve. {0}", instructionsToIgnore );
                return false;
            }

            // Allow a 1 day margin of error since this is not meant to be restrictive (just prevent big errors)
            var yesterday = RockDateTime.Now.AddDays( -1 );
            var tomorrow = RockDateTime.Now.AddDays( 1 );

            if ( tomorrow < _financialScheduledTransaction.StartDate )
            {
                errorMessage = string.Format( "The schedule start date is in the future. {0}", instructionsToIgnore );
                return false;
            }

            if ( _financialScheduledTransaction.EndDate.HasValue && yesterday > _financialScheduledTransaction.EndDate )
            {
                errorMessage = string.Format( "The schedule end date is in the past. {0}", instructionsToIgnore );
                return false;
            }

            if ( _financialScheduledTransaction.NumberOfPayments.HasValue )
            {
                if ( !_currentNumberOfPaymentsForSchedule.HasValue )
                {
                    _currentNumberOfPaymentsForSchedule = _financialTransactionService.Queryable()
                        .AsNoTracking()
                        .Count( t => t.ScheduledTransactionId == _financialScheduledTransaction.Id );
                }
                
                if ( _currentNumberOfPaymentsForSchedule.Value >= _financialScheduledTransaction.NumberOfPayments.Value )
                {
                    errorMessage = string.Format( "The scheduled transaction already has the maximum number of occurence. {0}", instructionsToIgnore );
                    return false;
                }
            }

            // The idea here is to provide protection but not be overly restrictive.
            // Given requirement example: for a weekly schedule, check if there was a transaction in the last 3 days
            // From that example I derived the 40% accuracy factor
            var minDateTime = RockDateTime.Now;
            var accuracyFactor = 0.4d;

            // I used 28 for the number of days in a month to keep calculations simple since that is the smallest month
            // I used AddDays since it accepts doubles
            switch ( _financialScheduledTransaction.TransactionFrequencyValue.Guid.ToString().ToUpper() )
            {
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME:
                    minDateTime = DateTime.MinValue;
                    break;
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY:
                    minDateTime = minDateTime.AddDays( -7 * accuracyFactor );
                    break;
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY:
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY:
                    minDateTime = minDateTime.AddDays( -14 * accuracyFactor );
                    break;
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY:
                    minDateTime = minDateTime.AddDays( -1 * accuracyFactor );
                    break;
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_QUARTERLY:
                    minDateTime = minDateTime.AddDays( -3 * 28 * accuracyFactor );
                    break;
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEYEARLY:
                    minDateTime = minDateTime.AddDays( -6 * 28 * accuracyFactor );
                    break;
                case SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_YEARLY:
                    minDateTime = minDateTime.AddDays( -364 * accuracyFactor );
                    break;
                default:
                    errorMessage = string.Format(
                        "The scheduled transaction frequency ID is not valid: {0}. {1}",
                        _financialScheduledTransaction.TransactionFrequencyValueId,
                        instructionsToIgnore );
                    return false;
            }

            var previousOccurrenceTransaction = _financialTransactionService.Queryable()
                .AsNoTracking()
                .Where( t => t.ScheduledTransactionId == _financialScheduledTransaction.Id )
                .Where( t => t.TransactionDateTime.HasValue && t.TransactionDateTime.Value >= minDateTime )
                .FirstOrDefault();

            if ( previousOccurrenceTransaction != null )
            {
                errorMessage = string.Format(
                    "The schedule seems to have already been processed for the given frequency on {0}. Check transaction ID: {1}. {2}",
                    previousOccurrenceTransaction.TransactionDateTime.Value.ToShortDateString(),
                    previousOccurrenceTransaction.Id,
                    instructionsToIgnore );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the arguments supplied to the constructor. Entities are loaded from supplied IDs where applicable to ensure existance and a valid state.
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if arguments are valid. Otherwise a message will be set indicating the problem.</param>
        /// <returns>True if the arguments are valid. False otherwise.</returns>
        public bool AreArgsValid( out string errorMessage )
        {
            errorMessage = string.Empty;

            LoadEntities();

            if ( _authorizedPerson == null )
            {
                errorMessage = "The authorizedPersonAliasId did not resolve to a person";
                return false;
            }

            if ( _financialGateway == null )
            {
                errorMessage = "The gatewayId did not resolve";
                return false;
            }

            if ( !_financialGateway.IsActive )
            {
                errorMessage = "The gateway is not active";
                return false;
            }

            if ( _automatedGatewayComponent as IAutomatedGatewayComponent == null )
            {
                errorMessage = "The gateway failed to produce an automated gateway component";
                return false;
            }

            if ( _automatedPaymentArgs.AutomatedPaymentDetails == null || !_automatedPaymentArgs.AutomatedPaymentDetails.Any() )
            {
                errorMessage = "At least one item is required in the TransactionDetails";
                return false;
            }

            if ( _financialAccounts.Count != _automatedPaymentArgs.AutomatedPaymentDetails.Count )
            {
                errorMessage = "Each detail must reference a unique financial account";
                return false;
            }

            var totalAmount = 0m;

            foreach ( var detail in _automatedPaymentArgs.AutomatedPaymentDetails )
            {
                if ( detail.Amount <= 0m )
                {
                    errorMessage = "The detail amount must be greater than $0";
                    return false;
                }

                var financialAccount = _financialAccounts[detail.AccountId];

                if ( financialAccount == null )
                {
                    errorMessage = string.Format( "The accountId '{0}' did not resolve", detail.AccountId );
                    return false;
                }

                if ( !financialAccount.IsActive )
                {
                    errorMessage = string.Format( "The account '{0}' is not active", detail.AccountId );
                    return false;
                }

                totalAmount += detail.Amount;
            }

            if ( totalAmount < 1m )
            {
                errorMessage = "The total amount must be at least $1";
                return false;
            }

            if ( _financialPersonSavedAccount == null && _automatedPaymentArgs.FinancialPersonSavedAccountId.HasValue )
            {
                errorMessage = string.Format(
                    "The saved account '{0}' is not valid for the person or gateway",
                    _automatedPaymentArgs.FinancialPersonSavedAccountId.Value );
                return false;
            }

            if ( _financialPersonSavedAccount == null )
            {
                errorMessage = string.Format( "The given person does not have a saved account for this gateway" );
                return false;
            }

            if ( _referencePaymentInfo == null )
            {
                errorMessage = string.Format( "The saved account failed to produce reference payment info" );
                return false;
            }

            if ( _transactionType == null )
            {
                errorMessage = string.Format( "The transaction type is invalid" );
                return false;
            }

            if ( _financialSource == null )
            {
                errorMessage = string.Format( "The financial source is invalid" );
                return false;
            }

            if ( _automatedPaymentArgs.ScheduledTransactionId.HasValue && _financialScheduledTransaction == null )
            {
                errorMessage = string.Format( "The scheduled transaction did not resolve" );
                return false;
            }

            if ( _financialScheduledTransaction != null && _financialScheduledTransaction.AuthorizedPersonAliasId != _automatedPaymentArgs.AuthorizedPersonAliasId )
            {
                errorMessage = string.Format( "The scheduled transaction and authorized person alias ID are not valid together" );
                return false;
            }

            if ( _financialScheduledTransaction != null && _financialScheduledTransaction.FinancialGatewayId != _automatedPaymentArgs.AutomatedGatewayId )
            {
                errorMessage = string.Format( "The scheduled transaction and gateway ID are not valid together" );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the arguments, changes the payment to the gateway, and then stores the resulting transaction in the database.
        /// </summary>
        /// <param name="errorMessage">Will be set to empty string if arguments are valid and payment succeeds. Otherwise a message will be set indicating the problem.</param>
        /// <returns>The resulting transaction which has been stored in the database</returns>
        public FinancialTransaction ProcessCharge( out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( _financialTransaction != null )
            {
                errorMessage = "A transaction has already been produced";
                return null;
            }

            if ( IsRepeatCharge( out errorMessage ) )
            {
                return null;
            }

            if ( !IsAccordingToSchedule( out errorMessage ) )
            {
                return null;
            }

            if ( !AreArgsValid( out errorMessage ) )
            {
                return null;
            }

            _referencePaymentInfo.Amount = _automatedPaymentArgs.AutomatedPaymentDetails.Sum( d => d.Amount );
            _referencePaymentInfo.Email = _authorizedPerson.Email;

            _financialTransaction = ( _automatedGatewayComponent as IAutomatedGatewayComponent ).AutomatedCharge( _financialGateway, _referencePaymentInfo, out errorMessage );

            if ( !string.IsNullOrEmpty( errorMessage ) )
            {
                errorMessage = string.Format( "Error charging: {0}", errorMessage );
                return null;
            }

            if ( _financialTransaction == null )
            {
                errorMessage = "Error charging: transaction was not created";
                return null;
            }

            SaveTransaction();

            return _financialTransaction;
        }

        /// <summary>
        /// Safely load entities that have not yet been assigned a non-null value based on the arguments.
        /// </summary>
        private void LoadEntities()
        {
            if ( _automatedPaymentArgs.ScheduledTransactionId.HasValue && _financialScheduledTransaction == null )
            {
                _financialScheduledTransaction = _financialScheduledTransactionService.Queryable()
                    .AsNoTracking()
                    .Include( s => s.TransactionFrequencyValue )
                    .FirstOrDefault( s => s.Id == _automatedPaymentArgs.ScheduledTransactionId.Value );
            }

            if ( _authorizedPerson == null )
            {
                _authorizedPerson = _personAliasService.GetPersonNoTracking( _automatedPaymentArgs.AuthorizedPersonAliasId );
            }

            if ( _financialGateway == null )
            {
                _financialGateway = _financialGatewayService.GetNoTracking( _automatedPaymentArgs.AutomatedGatewayId );
            }

            if ( _financialGateway != null && _automatedGatewayComponent == null )
            {
                _automatedGatewayComponent = _financialGateway.GetGatewayComponent();
            }

            if ( _financialAccounts == null )
            {
                var accountIds = _automatedPaymentArgs.AutomatedPaymentDetails.Select( d => d.AccountId ).ToList();
                _financialAccounts = _financialAccountService.GetByIds( accountIds ).AsNoTracking().ToDictionary( fa => fa.Id, fa => fa );
            }

            if ( _authorizedPerson != null && _financialPersonSavedAccount == null && _financialGateway != null )
            {
                // Pick the correct saved account based on args or default for the user
                var financialGatewayId = _financialGateway.Id;

                var savedAccountQry = _financialPersonSavedAccountService
                    .GetByPersonId( _authorizedPerson.Id )
                    .AsNoTracking()
                    .Where( sa => sa.FinancialGatewayId == financialGatewayId )
                    .Include( sa => sa.FinancialPaymentDetail );

                if ( _automatedPaymentArgs.FinancialPersonSavedAccountId.HasValue )
                {
                    // If there is an indicated saved account to use, don't assume any other saved account even with a schedule
                    var savedAccountId = _automatedPaymentArgs.FinancialPersonSavedAccountId.Value;
                    _financialPersonSavedAccount = savedAccountQry.FirstOrDefault( sa => sa.Id == savedAccountId );
                }
                else if ( _financialScheduledTransaction != null )
                {
                    // If there is a schedule and no indicated saved account to use, use payment info associated with the schedule
                    _financialPersonSavedAccount = savedAccountQry
                        .Where( sa =>
                            ( sa.FinancialPaymentDetailId.HasValue && sa.FinancialPaymentDetailId == _financialScheduledTransaction.FinancialPaymentDetailId ) ||
                            ( !string.IsNullOrEmpty( sa.TransactionCode ) && sa.TransactionCode == _financialScheduledTransaction.TransactionCode )
                        )
                        .FirstOrDefault();
                }
                else
                {
                    // Use the default or first if no default
                    _financialPersonSavedAccount = savedAccountQry.FirstOrDefault( sa => sa.IsDefault ) ?? savedAccountQry.FirstOrDefault();
                }
            }

            if ( _financialPersonSavedAccount != null && _referencePaymentInfo == null )
            {
                _referencePaymentInfo = _financialPersonSavedAccount.GetReferencePayment();
            }

            if ( _transactionType == null )
            {
                _transactionType = DefinedValueCache.Get( _automatedPaymentArgs.TransactionTypeGuid ?? SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );
            }

            if ( _financialSource == null )
            {
                _financialSource = DefinedValueCache.Get( _automatedPaymentArgs.FinancialSourceGuid ?? SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE.AsGuid() );
            }            
        }

        /// <summary>
        /// Once _financialTransaction is set, this method stores the transaction in the database along with the appropriate details and batch information.
        /// </summary>
        private void SaveTransaction()
        {
            if ( _financialTransaction.Guid.Equals( default( Guid ) ) )
            {
                _financialTransaction.Guid = Guid.NewGuid();
            }

            _financialTransaction.CreatedByPersonAliasId = _currentPersonAliasId;
            _financialTransaction.ScheduledTransactionId = _automatedPaymentArgs.ScheduledTransactionId;
            _financialTransaction.AuthorizedPersonAliasId = _automatedPaymentArgs.AuthorizedPersonAliasId;
            _financialTransaction.ShowAsAnonymous = _automatedPaymentArgs.ShowAsAnonymous;
            _financialTransaction.TransactionDateTime = RockDateTime.Now;
            _financialTransaction.FinancialGatewayId = _financialGateway.Id;
            _financialTransaction.TransactionTypeValueId = _transactionType.Id;
            _financialTransaction.Summary = _referencePaymentInfo.Comment1;
            _financialTransaction.SourceTypeValueId = _financialSource.Id;

            if ( _financialTransaction.FinancialPaymentDetail == null )
            {
                _financialTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }

            _financialTransaction.FinancialPaymentDetail.SetFromPaymentInfo( _referencePaymentInfo, _automatedGatewayComponent, _rockContext );

            foreach ( var detailArgs in _automatedPaymentArgs.AutomatedPaymentDetails )
            {
                var transactionDetail = new FinancialTransactionDetail();
                transactionDetail.Amount = detailArgs.Amount;
                transactionDetail.AccountId = detailArgs.AccountId;

                _financialTransaction.TransactionDetails.Add( transactionDetail );
            }

            var batch = _financialBatchService.Get(
                _automatedPaymentArgs.BatchNamePrefix ?? "Online Giving",
                _referencePaymentInfo.CurrencyTypeValue,
                _referencePaymentInfo.CreditCardTypeValue,
                _financialTransaction.TransactionDateTime.Value,
                _financialGateway.GetBatchTimeOffset() );

            var batchChanges = new History.HistoryChangeList();

            if ( batch.Id == 0 )
            {
                batchChanges.AddChange( History.HistoryVerb.Add, History.HistoryChangeType.Record, "Batch" );
                History.EvaluateChange( batchChanges, "Batch Name", string.Empty, batch.Name );
                History.EvaluateChange( batchChanges, "Status", null, batch.Status );
                History.EvaluateChange( batchChanges, "Start Date/Time", null, batch.BatchStartDateTime );
                History.EvaluateChange( batchChanges, "End Date/Time", null, batch.BatchEndDateTime );
            }

            var newControlAmount = batch.ControlAmount + _financialTransaction.TotalAmount;
            History.EvaluateChange( batchChanges, "Control Amount", batch.ControlAmount.FormatAsCurrency(), newControlAmount.FormatAsCurrency() );
            batch.ControlAmount = newControlAmount;

            _financialTransaction.BatchId = batch.Id;
            _financialTransaction.LoadAttributes( _rockContext );

            batch.Transactions.Add( _financialTransaction );

            _rockContext.SaveChanges();
            _financialTransaction.SaveAttributeValues();

            HistoryService.SaveChanges(
                _rockContext,
                typeof( FinancialBatch ),
                SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                batch.Id,
                batchChanges
            );
        }
    }
}
