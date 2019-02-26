using System;
using System.IO;
using Menu.Logger;
using FluentScheduler;

namespace Menu.Functional.Tasker
{
    public class TaskManager
    {
        public static string currectDirectory = "";
        private LogProgram logger;

        public TaskManager(LogProgram logProgram)
        {
            this.logger = logProgram;
            SetNewLogFile();
        }
        private void SetNewLogFile()
        {
            Registry registry = new Registry();
            registry.Schedule(() => logger.SetUpNewLogFile()).ToRunEvery(1).Days().At(10, 0);
            logger.WriteLog("Task manager start Daily_Regimen method", LogLevel.Tasker);
            JobManager.InitializeWithoutStarting(registry);
            JobManager.Start();
        }
    }
}
