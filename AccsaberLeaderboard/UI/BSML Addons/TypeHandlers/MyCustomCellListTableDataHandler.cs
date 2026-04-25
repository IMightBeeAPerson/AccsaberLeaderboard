using AccsaberLeaderboard.UI.BSML_Addons.Components;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;
using HMUI;
using IPA.Config.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AccsaberLeaderboard.UI.BSML_Addons.TypeHandlers
{
    [ComponentHandler(typeof(MyCustomCellListTableDataHandler))]
    internal class MyCustomCellListTableDataHandler : TypeHandler
    {
        public override Dictionary<string, string[]> Props => new()
        {
            { "selectCell", [ "select-cell" ] },
            { "data", [ "contents", "data" ] },
            { "cellClickable", [ "clickable-cells" ] }
        };

        public override void HandleType(BSMLParser.ComponentTypeWithData componentType, BSMLParserParams parserParams)
        {
#if NEW_VERSION
            ref Component component = ref componentType.Component;
            ref Dictionary<string, string> data = ref componentType.Data;
            ref Dictionary<string, BSMLValue> values = ref parserParams.Values;
#else
            ref Component component = ref componentType.component;
            ref Dictionary<string, string> data = ref componentType.data;
            ref Dictionary<string, BSMLValue> values = ref parserParams.values;
#endif

            MyCustomCellListTableData componentData = component as MyCustomCellListTableData;

            if (data.TryGetValue("selectCell", out string selectCell))
            {
                componentData.OnCellClick += index =>
                {
                    Plugin.Log.Info("Click Detected!");
                    if (!parserParams.actions.TryGetValue(selectCell, out BSMLAction action))
                    {
                        throw new Exception("select-cell action '" + selectCell + "' not found");
                    }

                    action.Invoke(componentData.Data[index]);
                };
            }

            if (data.TryGetValue("data", out string dataStr))
            {
                if (!values.TryGetValue(dataStr, out BSMLValue contents))
                    throw new Exception("value '" + dataStr + "' not found");

                if (contents.GetValue() is not List<ICellDataSource> cells)
                    throw new Exception($"Value '{dataStr}' is not a List<ICellDataSource>, which is required for my-custom-list");

                componentData.Data = cells;
            }

            if (data.TryGetValue("cellClickable", out string cellClickable))
                componentData.ClickableCells = Parse.Bool(cellClickable);

        }
    }
}
