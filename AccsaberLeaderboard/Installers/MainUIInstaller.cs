using AccsaberLeaderboard.UI;
using AccsaberLeaderboard.UI.ViewControllers;
using Zenject;

namespace AccsaberLeaderboard.Installers
{
    public class MainUIInstaller : Installer<MainUIInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<UI.ViewControllers.LeaderboardViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<PanelViewController>().FromNewComponentAsViewController().AsSingle();

            Container.BindInterfacesAndSelfTo<MainLeaderboardController>().AsSingle();
        }
    }
}
