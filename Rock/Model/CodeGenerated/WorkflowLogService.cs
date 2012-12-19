//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//

using System;
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// WorkflowLog Service class
    /// </summary>
    public partial class WorkflowLogService : Service<WorkflowLog>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowLogService"/> class
        /// </summary>
        public WorkflowLogService()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowLogService"/> class
        /// </summary>
        public WorkflowLogService(IRepository<WorkflowLog> repository) : base(repository)
        {
        }

        /// <summary>
        /// Determines whether this instance can delete the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( WorkflowLog item, out string errorMessage )
        {
            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class WorkflowLogExtension
    {
        /// <summary>
        /// To the dto.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static WorkflowLog Clone( this WorkflowLog entity )
        {
            var newEntity = new WorkflowLog();

            newEntity.WorkflowId = entity.WorkflowId;
            newEntity.LogDateTime = entity.LogDateTime;
            newEntity.LogText = entity.LogText;
            newEntity.Id = entity.Id;
            newEntity.Guid = entity.Guid;

            return newEntity;
        }

    }
}