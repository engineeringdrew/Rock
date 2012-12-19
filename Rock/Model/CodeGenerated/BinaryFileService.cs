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
    /// BinaryFile Service class
    /// </summary>
    public partial class BinaryFileService : Service<BinaryFile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryFileService"/> class
        /// </summary>
        public BinaryFileService()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryFileService"/> class
        /// </summary>
        public BinaryFileService(IRepository<BinaryFile> repository) : base(repository)
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
        public bool CanDelete( BinaryFile item, out string errorMessage )
        {
            errorMessage = string.Empty;
 
            if ( new Service<Category>().Queryable().Any( a => a.FileId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", BinaryFile.FriendlyTypeName, Category.FriendlyTypeName );
                return false;
            }  
 
            if ( new Service<Page>().Queryable().Any( a => a.IconFileId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", BinaryFile.FriendlyTypeName, Page.FriendlyTypeName );
                return false;
            }  
 
            if ( new Service<Person>().Queryable().Any( a => a.PhotoId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", BinaryFile.FriendlyTypeName, Person.FriendlyTypeName );
                return false;
            }  
 
            if ( new Service<WorkflowType>().Queryable().Any( a => a.FileId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", BinaryFile.FriendlyTypeName, WorkflowType.FriendlyTypeName );
                return false;
            }  
            return true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class BinaryFileExtension
    {
        /// <summary>
        /// To the dto.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static BinaryFile Clone( this BinaryFile entity )
        {
            var newEntity = new BinaryFile();

            newEntity.IsTemporary = entity.IsTemporary;
            newEntity.IsSystem = entity.IsSystem;
            newEntity.Data = entity.Data;
            newEntity.Url = entity.Url;
            newEntity.FileName = entity.FileName;
            newEntity.MimeType = entity.MimeType;
            newEntity.LastModifiedTime = entity.LastModifiedTime;
            newEntity.Description = entity.Description;
            newEntity.Id = entity.Id;
            newEntity.Guid = entity.Guid;

            return newEntity;
        }

    }
}