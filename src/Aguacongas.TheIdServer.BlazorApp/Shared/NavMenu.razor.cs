﻿namespace Aguacongas.TheIdServer.BlazorApp.Shared
{
    public partial class NavMenu
    {
        bool collapseNavMenu = true;

        string NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }
    }
}
