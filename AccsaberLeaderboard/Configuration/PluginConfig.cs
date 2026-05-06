
using System.Runtime.CompilerServices;
using AccsaberLeaderboard.Counter.Utils;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace AccsaberLeaderboard.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        public virtual bool CombineRelations { get; set; } = false;

        #region Counter Settings
        public virtual int DecimalPlaces { get; set; } = 2;
        public virtual float FontSize { get; set; } = 3f;
        [UseConverter]
        public virtual CounterModes CounterMode { get; set; } = CounterModes.Normal;

        #endregion



        /*
                /// <summary>
                /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
                /// </summary>
                public virtual void OnReload()
                {
                    // Do stuff after config is read from disk.
                }

                /// <summary>
                /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
                /// </summary>
                public virtual void Changed()
                {
                    // Do stuff when the config is changed.
                }

                /// <summary>
                /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
                /// </summary>
                public virtual void CopyFrom(PluginConfig other)
                {
                    // This instance's members populated from other
                }*/
    }
}
