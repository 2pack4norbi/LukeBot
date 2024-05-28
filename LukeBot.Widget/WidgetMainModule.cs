using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Widget.Common;
using Intercom = LukeBot.Communication.Common.Intercom;


namespace LukeBot.Widget
{
    public class WidgetMainModule: IMainModule
    {
        private Dictionary<string, WidgetUserModule> mUsers = new();
        private Dictionary<string, string> mWidgetIDToUser = new();
        private Mutex mMutex = new();


        Intercom::ResponseBase ResponseAllocator(Intercom::MessageBase msg)
        {
            switch (msg.Message)
            {
            case Messages.GET_WIDGET_PAGE: return new GetWidgetPageResponse();
            case Messages.ASSIGN_WS: return new AssignWSResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new Intercom::ResponseBase();
        }

        void GetWidgetPageDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            GetWidgetPageMessage m = (GetWidgetPageMessage)msg;
            GetWidgetPageResponse r = (GetWidgetPageResponse)resp;

            mMutex.WaitOne();

            string ret;

            try
            {
                string user = mWidgetIDToUser[m.widgetID];
                ret = mUsers[user].GetWidgetPage(m.widgetID);
            }
            catch (System.Exception e)
            {
                mMutex.ReleaseMutex();
                r.SignalError(String.Format("Widget {0} couldn't be acquired: {1}", m.widgetID, e.Message));
                return;
            }

            mMutex.ReleaseMutex();

            r.SetContents(ret);
            r.SignalSuccess();
        }

        void AssignWSDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            AssignWSMessage m = (AssignWSMessage)msg;
            AssignWSResponse r = (AssignWSResponse)resp;

            mMutex.WaitOne();

            try
            {
                string user = mWidgetIDToUser[m.widgetID];
                r.SetLifetimeTask(mUsers[user].AssignWS(m.widgetID, m.GetWebSocket()));
            }
            catch (System.Exception e)
            {
                mMutex.ReleaseMutex();
                r.SignalError(String.Format("Widget {0} WS assignment failed: {1}", m.widgetID, e.Message));
                return;
            }

            mMutex.ReleaseMutex();
            resp.SignalSuccess();
        }


        // User Module Descriptor delegates //

        private IUserModule UserModuleLoader(string lbUser)
        {
            return LoadWidgetUserModule(lbUser);
        }

        private void UserModuleUnloader(IUserModule module)
        {
            // ...
        }



        // Public methods //

        public WidgetMainModule()
        {
            Intercom::EndpointInfo widgetManagerInfo = new Intercom::EndpointInfo(Endpoints.WIDGET_MANAGER, ResponseAllocator);
            widgetManagerInfo.AddMessage(Messages.GET_WIDGET_PAGE, GetWidgetPageDelegate);
            widgetManagerInfo.AddMessage(Messages.ASSIGN_WS, AssignWSDelegate);

            Comms.Intercom.Register(widgetManagerInfo);
        }

        public WidgetUserModule LoadWidgetUserModule(string lbUser)
        {
            if (mUsers.ContainsKey(lbUser))
            {
                throw new WidgetUserAlreadyLoadedException("Widget user {0} already loaded", lbUser);
            }

            WidgetUserModule user = new WidgetUserModule(lbUser);

            Logger.Log().Debug("Got {0} widgets for user {1}", user.ListWidgets().Count, lbUser);

            mUsers.Add(lbUser, user);
            foreach (WidgetDesc wd in user.ListWidgets())
                mWidgetIDToUser.Add(wd.Id, lbUser);

            Logger.Log().Info("Loaded Widgets for user {0}", lbUser);
            return user;
        }

        public string AddWidget(string lbUser, WidgetType type, string name)
        {
            IWidget w = mUsers[lbUser].AddWidget(type, name);
            mWidgetIDToUser.Add(w.ID, lbUser);
            return w.GetWidgetAddress();
        }

        public List<WidgetDesc> ListUserWidgets(string lbUser)
        {
            return mUsers[lbUser].ListWidgets();
        }

        public WidgetDesc GetWidgetInfo(string lbUser, string id)
        {
            return mUsers[lbUser].GetWidgetInfo(id);
        }

        public void DeleteWidget(string lbUser, string id)
        {
            mUsers[lbUser].DeleteWidget(id);
        }

        public void UpdateWidgetConfiguration(string lbUser, string id, IEnumerable<(string, string)> changes)
        {
            mUsers[lbUser].UpdateWidgetConfiguration(id, changes);
        }

        public WidgetConfiguration GetWidgetConfiguration(string lbUser, string id)
        {
            return mUsers[lbUser].GetWidgetConfiguration(id);
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            UserModuleDescriptor umd = new UserModuleDescriptor();
            umd.Type = ModuleType.Widget;
            umd.LoadPrerequisite = null;
            umd.Loader = UserModuleLoader;
            umd.Unloader = UserModuleUnloader;
            return umd;
        }

        public void Run()
        {
        }

        public void RequestShutdown()
        {
            foreach (WidgetUserModule um in mUsers.Values)
                um.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (WidgetUserModule um in mUsers.Values)
                um.WaitForShutdown();
        }
    }
}