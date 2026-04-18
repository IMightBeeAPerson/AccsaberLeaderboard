using System.Reflection;
using Zenject;

namespace AccsaberLeaderboard.Installers
{
    public static class InstallerHelper
    {
        private static readonly PropertyInfo containerPI = typeof(MonoInstallerBase).GetProperty("Container", BindingFlags.NonPublic | BindingFlags.Instance);

        public static DiContainer GetContainer(this MonoInstallerBase monoInstallerBase) => (DiContainer)containerPI.GetValue(monoInstallerBase);
    }
}
