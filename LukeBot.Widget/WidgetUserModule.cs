using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public class WidgetUserModule: IUserModule
    {
        private Dictionary<string, IWidget> mWidgets = new();
        private Dictionary<string, string> mNameToId = new();
        private string mLBUser;


        private Path GetWidgetCollectionPropertyName()
        {
            return Path.Start()
                .Push(LukeBot.Common.Constants.PROP_STORE_USER_DOMAIN)
                .Push(mLBUser)
                .Push(LukeBot.Common.Constants.WIDGET_MODULE_NAME)
                .Push(Constants.PROP_WIDGETS);
        }

        private void LoadWidgetsFromConfig()
        {
            Path widgetCollectionProp = GetWidgetCollectionPropertyName();

            WidgetDesc[] widgets;
            if (!Conf.TryGet<WidgetDesc[]>(widgetCollectionProp, out widgets))
                return; // quiet exit, assume user does not have any widgets

            foreach (WidgetDesc wd in widgets)
            {
                AddWidgetFromDesc(wd);
            }
        }

        private WidgetDesc GetWidgetDescFromConfig(string id)
        {
            Path widgetCollectionProp = GetWidgetCollectionPropertyName();

            WidgetDesc[] widgets;
            if (!Conf.TryGet<WidgetDesc[]>(widgetCollectionProp, out widgets))
                throw new WidgetNotFoundException(id);

            foreach (WidgetDesc wd in widgets)
            {
                if (wd.Id == id)
                    return wd;
            }

            throw new WidgetNotFoundException(id);
        }

        private void SaveWidgetToConfig(IWidget w)
        {
            WidgetDesc wd = w.GetDesc();

            Path widgetCollectionProp = GetWidgetCollectionPropertyName();
            ConfUtil.ArrayAppend(widgetCollectionProp, wd, new WidgetDesc.Comparer());
        }

        private void RemoveWidgetFromConfig(string id)
        {
            Path widgetCollectionProp = GetWidgetCollectionPropertyName();
            ConfUtil.ArrayRemove<WidgetDesc>(widgetCollectionProp, (WidgetDesc d) => d.Id != id);
        }



        private IWidget AddWidgetFromDesc(WidgetDesc wd)
        {
            IWidget w = AllocateWidget(wd.Type, wd.Id, wd.Name);
            mWidgets.Add(wd.Id, w);

            if (wd.Name != null && wd.Name.Length > 0)
                mNameToId.Add(wd.Name, wd.Id);

            LoadWidget(wd.Id);

            return w;
        }

        private IWidget AllocateWidget(WidgetType type, string id, string name)
        {
            switch (type)
            {
            case WidgetType.echo: return new Echo(mLBUser, id, name);
            case WidgetType.nowplaying: return new NowPlaying(mLBUser, id, name);
            case WidgetType.chat: return new Chat(mLBUser, id, name);
            case WidgetType.alerts: return new Alerts(mLBUser, id, name);
            default:
                throw new InvalidWidgetTypeException("Invalid widget type: {0}", type);
            }
        }

        private void RemoveWidget(string id)
        {
            string name = mWidgets[id].Name;

            UnloadWidget(id);
            mWidgets.Remove(id);

            if (mNameToId.ContainsKey(name))
                mNameToId.Remove(name);

            RemoveWidgetFromConfig(id);
        }

        private void LoadWidget(string id)
        {
            IWidget w = mWidgets[id];

            try
            {
                // Load() can fail, which will leave the Widget in unloaded state.
                w.Load();
            }
            catch (Exception e)
            {
                if (w.Name.Length > 0)
                    Logger.Log().Error("Falied to load Widget {0} ({1}): {2}", w.Name, w.ID, e.Message);
                else
                    Logger.Log().Error("Falied to load Widget {0}: {1}", w.ID, e.Message);
            }
        }

        private void UnloadWidget(string id)
        {
            IWidget w = mWidgets[id];

            try
            {
                // Unload() can fail, but we should ignore it and move on
                w.Unload();
            }
            catch (Exception e)
            {
                if (w.Name.Length > 0)
                    Logger.Log().Error("Falied to unload Widget {0} ({1}): {2}", w.Name, w.ID, e.Message);
                else
                    Logger.Log().Error("Falied to unload Widget {0}: {1}", w.ID, e.Message);
            }
        }

        internal string GetWidgetPage(string widgetID)
        {
            if (!mWidgets.TryGetValue(widgetID, out IWidget widget))
                throw new WidgetNotFoundException(widgetID);

            return widget.GetPage();
        }

        internal Task AssignWS(string widgetID, WebSocket ws)
        {
            if (!mWidgets.TryGetValue(widgetID, out IWidget widget))
            {
                throw new WidgetNotFoundException(widgetID);
            }

            return widget.AcquireWS(ws);
        }

        // tries to see if provided ID is a widget ID.
        // If it isn't a key in Widgets dictionary, tries to fetch the ID
        // assuming this is a short-hand name.
        // With nothing found throws an exception.
        internal string GetActualWidgetId(string id)
        {
            if (mWidgets.ContainsKey(id))
                return id;

            // not an id in widgets dict, try cross-checking it with user friendly names
            if (!mNameToId.TryGetValue(id, out string actualId))
                throw new WidgetNotFoundException(id);

            return actualId;
        }


        // Public methods //

        public WidgetUserModule(string lbUser)
        {
            mLBUser = lbUser;

            LoadWidgetsFromConfig();
        }

        ~WidgetUserModule()
        {
        }

        public void Init()
        {
        }

        public void Run()
        {
        }

        public IWidget AddWidget(WidgetType type, string name)
        {
            if (mNameToId.ContainsKey(name))
                throw new WidgetAlreadyExistsException(name, mNameToId[name]);

            string id = Guid.NewGuid().ToString();

            IWidget w = AllocateWidget(type, id, name);
            mWidgets.Add(id, w);

            if (name != null && name.Length > 0)
                mNameToId.Add(name, id);

            SaveWidgetToConfig(w);

            LoadWidget(id);

            return w;
        }

        public List<WidgetDesc> ListWidgets()
        {
            List<WidgetDesc> widgets = new List<WidgetDesc>();

            foreach (IWidget w in mWidgets.Values)
            {
                widgets.Add(w.GetDesc());
            }

            return widgets;
        }

        public WidgetDesc GetWidgetInfo(string id)
        {
            return mWidgets[GetActualWidgetId(id)].GetDesc();
        }

        public bool IsWidgetLoaded(string id)
        {
            return mWidgets[GetActualWidgetId(id)].Loaded;
        }

        public void DeleteWidget(string id)
        {
            string actualId = GetActualWidgetId(id);

            RemoveWidget(actualId);
        }

        public void ReloadWidget(string id)
        {
            string actualId = GetActualWidgetId(id);

            UnloadWidget(actualId);
            LoadWidget(actualId);
        }

        public WidgetConfiguration GetWidgetConfiguration(string id)
        {
            return mWidgets[GetActualWidgetId(id)].GetConfig();
        }

        public void UpdateWidgetConfiguration(string id, IEnumerable<(string, string)> changes)
        {
            IWidget w = mWidgets[GetActualWidgetId(id)];
            w.ValidateConfigUpdate(changes);
            w.UpdateConfig(changes);
        }

        public void RequestShutdown()
        {
            foreach (IWidget w in mWidgets.Values)
            {
                string id = w.ID;
                UnloadWidget(id);
            }
        }

        public void WaitForShutdown()
        {
        }

        public ModuleType GetModuleType()
        {
            return ModuleType.Widget;
        }
    }
}
