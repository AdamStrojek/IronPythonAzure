using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using IronPython;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace IronPython.Azure.ServiceRuntime
{
    public class PyRoleEntryPoint : RoleEntryPoint
    {
        public string DefaultScriptName = "worker.py";

        private ScriptEngine engine;
        private ScriptScope scope;

        Func<bool> pyStart;
        Action pyStop, pyRun;

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("$projectname$ entry point called", "Information");

            this.pyRun();
        }

        public override bool OnStart()
        {
            string scriptName = RoleEnvironment.GetConfigurationSettingValue("ScriptName") ?? DefaultScriptName;

            InitScripting(scriptName);

            return (this.pyStart != null ? this.pyStart() : true) && base.OnStart();
        }

        private void InitScripting(string scriptName)
        {
            this.engine = Python.CreateEngine();
            this.engine.Runtime.LoadAssembly(typeof(string).Assembly);
            this.engine.Runtime.LoadAssembly(typeof(DiagnosticMonitor).Assembly);
            this.engine.Runtime.LoadAssembly(typeof(RoleEnvironment).Assembly);
            this.engine.Runtime.LoadAssembly(typeof(Microsoft.WindowsAzure.CloudStorageAccount).Assembly);

            this.scope = this.engine.CreateScope();
            engine.CreateScriptSourceFromFile(scriptName).Execute(scope);

            if (scope.ContainsVariable("start"))
                this.pyStart = scope.GetVariable<Func<bool>>("start");

            this.pyRun = scope.GetVariable<Action>("run");

            if (scope.ContainsVariable("stop"))
                this.pyStop = scope.GetVariable<Action>("stop");
        }

        public override void OnStop()
        {
            if (this.pyStop != null)
                this.pyStop();

            base.OnStop();
        }
    }
}
