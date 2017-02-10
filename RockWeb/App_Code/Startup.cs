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
using Microsoft.Owin;
using Owin;
using System.Collections.Generic;
using Rock.Utility;
using System.Linq;
using Rock;
using Rock.Model;

[assembly: OwinStartup( typeof( RockWeb.Startup ) )]
namespace RockWeb
{
    public class Startup
    {
        public void Configuration( IAppBuilder app )
        {
            app.MapSignalR();

            // Find any plugins that implement IRockOwinStartup
            try
            {
                var startups = new Dictionary<int, List<IRockOwinStartup>>();
                foreach ( var startupType in Rock.Reflection.FindTypes( typeof( IRockOwinStartup ) ).Select( a => a.Value ).ToList() )
                {
                    var startup = Activator.CreateInstance( startupType ) as IRockOwinStartup;
                    startups.AddOrIgnore( startup.StartupOrder, new List<IRockOwinStartup>() );
                    startups[startup.StartupOrder].Add( startup );
                }

                foreach ( var startupList in startups.OrderBy( s => s.Key ).Select( s => s.Value ) )
                {
                    foreach ( var startup in startupList )
                    {
                        startup.OnStartup( app );
                    }
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, null );
            }
        }
    }
}
