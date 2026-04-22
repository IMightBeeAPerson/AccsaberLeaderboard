using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static AccsaberLeaderboard.API.AccsaberAPI;
using static AccsaberLeaderboard.Models.AccsaberMilestoneData;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    internal class PlayerMilestoneModalViewController
    {
#pragma warning disable IDE0044, IDE0051, IDE0052

        [UIParams] private BSMLParserParams parserParams;

        [UIObject("modal")] private GameObject modal;

        [UIObject("loader")] private GameObject loader;
        [UIObject("container")] private GameObject container;

        [UIComponent("list")] private CustomCellListTableData list;
        [UIValue("listValues")] private List<object> listValues = [];


        [UIValue("showModalName")] public const string showModalName = "ShowModalInfo";
        [UIValue("hideModalName")] public const string hideModalName = "HideModalInfo";

        [UIValue("fontSizeTitle")] public const float fontSizeTitle = 7f;

        [UIValue("modalWidth")] public const float modalWidth = 120f;
        [UIValue("modalHeight")] public const float modalHeight = 80f;

        [UIValue("listWidth")] public const float listWidth = 100f;
        [UIValue("listHeight")] public const float listHeight = 60f;
        [UIValue("cellSize")] public const float cellSize = 15f;
        [UIValue("listSpacerSize")] public const float listSpacerSize = (modalWidth - listWidth) / 2f;


        public PlayerMilestoneModalViewController(GameObject parent)
        {
            MiscUtils.Parse(ResourcePaths.BSML_MILESTONE_MODAL, parent.transform, this);
            modal.transform.SetParent(parent.transform);
        }
        

        public Task ShowMilestoneModal(Task<List<MilestoneInfoToken>> milestoneInfoLoader, MonoBehaviour host)
        {
            parserParams.EmitEvent(showModalName);

            host.StartCoroutine(ShowModalStart());

            return Task.Run(async () =>
            {
                host.StartCoroutine(ShowModal(await milestoneInfoLoader));
            });
        }
        public Task ShowMilestoneModal(string playerId, MonoBehaviour host)
        {
            return ShowMilestoneModal(Task.Run(() => ShowModalAsync(playerId)), host);
        }

        private Task<List<MilestoneInfoToken>> ShowModalAsync(string userId)
        {
            return GetMilestoneData(userId, completed: false, sorter: (a, b) =>
            {
                float diff = GetCalculatedProgress(b) - GetCalculatedProgress(a);
                return diff < 0 ? -1 : Mathf.Approximately(0f, diff) ? 0 : 1;
            });
        }

        private IEnumerator ShowModalStart()
        {
            yield return new WaitForEndOfFrame();

            (modal.transform as RectTransform).sizeDelta = new Vector2(modalWidth, modalHeight);
            loader.SetActive(true);
            container.SetActive(false);
        }
        private IEnumerator ShowModal(List<MilestoneInfoToken> sortedMilestones)
        {
            listValues = [.. sortedMilestones.Select(WrapData).Select(data => new AccsaberMilestoneDataInfo(data))];

#if NEW_VERSION
            list.Data = listValues;

            yield return new WaitUntil(() => list.Data.Count > 0);
            yield return new WaitForSeconds(0.1f);

            list.TableView.ReloadData();
#else
            list.data = listValues;

            yield return new WaitUntil(() => list.data.Count > 0);
            yield return new WaitForSeconds(0.1f);

            list.tableView.ReloadData();
#endif

            loader.SetActive(false);
            container.SetActive(true);

        }
    }
}
