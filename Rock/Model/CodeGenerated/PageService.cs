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
    /// Page Service class
    /// </summary>
    public partial class PageService : Service<Page>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageService"/> class
        /// </summary>
        public PageService()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageService"/> class
        /// </summary>
        public PageService(IRepository<Page> repository) : base(repository)
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
        public bool CanDelete( Page item, out string errorMessage )
        {
            errorMessage = string.Empty;
 
            if ( new Service<Page>().Queryable().Any( a => a.ParentPageId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", Page.FriendlyTypeName, Page.FriendlyTypeName );
                return false;
            }  
 
            if ( new Service<Site>().Queryable().Any( a => a.DefaultPageId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", Page.FriendlyTypeName, Site.FriendlyTypeName );
                return false;
            }  
            return true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class PageExtension
    {
        /// <summary>
        /// To the dto.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Page Clone( this Page entity )
        {
            var newEntity = new Page();

            newEntity.Name = entity.Name;
            newEntity.ParentPageId = entity.ParentPageId;
            newEntity.Title = entity.Title;
            newEntity.IsSystem = entity.IsSystem;
            newEntity.SiteId = entity.SiteId;
            newEntity.Layout = entity.Layout;
            newEntity.RequiresEncryption = entity.RequiresEncryption;
            newEntity.EnableViewState = entity.EnableViewState;
            newEntity.MenuDisplayDescription = entity.MenuDisplayDescription;
            newEntity.MenuDisplayIcon = entity.MenuDisplayIcon;
            newEntity.MenuDisplayChildPages = entity.MenuDisplayChildPages;
            newEntity.DisplayInNavWhen = entity.DisplayInNavWhen;
            newEntity.Order = entity.Order;
            newEntity.OutputCacheDuration = entity.OutputCacheDuration;
            newEntity.Description = entity.Description;
            newEntity.IconFileId = entity.IconFileId;
            newEntity.IncludeAdminFooter = entity.IncludeAdminFooter;
            newEntity.Id = entity.Id;
            newEntity.Guid = entity.Guid;

            return newEntity;
        }

    }
}