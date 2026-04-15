using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace AccsaberLeaderboard.Installers
{
    public static class InstallerHelper
    {
        private static readonly PropertyInfo containerPI = typeof(MonoInstallerBase).GetProperty("Container", BindingFlags.NonPublic | BindingFlags.Instance);

        public static DiContainer GetContainer(this MonoInstallerBase monoInstallerBase) => (DiContainer)containerPI.GetValue(monoInstallerBase);
    }
}
