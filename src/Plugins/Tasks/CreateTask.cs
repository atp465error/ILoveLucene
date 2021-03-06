using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Abstractions;
using Core.API;
using Newtonsoft.Json;

namespace Plugins.Tasks
{
    [Export(typeof(IActOnItem))]
    public class CreateTask : BaseActOnTypedItem<string>
    {
        [Import]
        public TaskRepository Repository { get; set; }

        public override void ActOn(string item)
        {
            if (item.Trim().StartsWith("new task "))
                item = item.Substring("new task ".Length);

            if (item.Trim().StartsWith("task "))
                item = item.Substring("task ".Length);

            Repository.CreateTask(new Task(item));
        }
    } 
    
    [Export(typeof(IActOnItem))]
    public class CreateAndStartTask : BaseActOnTypedItem<string>
    {
        [Import]
        public TaskRepository Repository { get; set; }

        public override void ActOn(string item)
        {
            if (item.Trim().StartsWith("new task "))
                item = item.Substring("new task ".Length);

            if (item.Trim().StartsWith("task "))
                item = item.Substring("task ".Length);

            var task = new Task(item);
            task.Start();
            Repository.CreateTask(task);
        }
    }

    [Export(typeof(IActOnItem))]
    public class StartTask : BaseActOnTypedItem<Task>, ICanActOnTypedItem<Task>
    {
        [Import]
        public TaskRepository Repository { get; set; }

        public override void ActOn(Task task)
        {
            task.Start();
            Repository.UpdateTask(task);
        }

        public bool CanActOn(Task item)
        {
            return !item.LastStartDate.HasValue;
        }
    }
    
    [Export(typeof(IActOnItem))]
    public class ArchiveTask : BaseActOnTypedItem<Task>, ICanActOnTypedItem<Task>
    {
        [Import]
        public TaskRepository Repository { get; set; }

        public override void ActOn(Task task)
        {
            Repository.ArchiveTask(task);
        }

        public bool CanActOn(Task item)
        {
            return !item.LastStartDate.HasValue;
        }
    }

    [Export(typeof(IActOnItem))]
    public class StopTask: BaseActOnTypedItem<Task>, ICanActOnTypedItem<Task>
    {
        [Import]
        public TaskRepository Repository { get; set; }

        public override void ActOn(Task task)
        {
            task.Stop();
            Repository.UpdateTask(task);
        }

        public bool CanActOn(Task item)
        {
            return item.LastStartDate.HasValue;
        }
    }

    [Export(typeof(IConverter))]
    public class TaskConverter : IConverter<Task>
    {
        [Import]
        public TaskRepository Repository { get; set; }

        public IItem FromDocumentToItem(CoreDocument document)
        {
            return Repository.FromFileName(document.GetString("filename"));
        }

        public string ToId(Task t)
        {
            return t.FileName;
        }

        public CoreDocument ToDocument(IItemSource itemSource, Task t)
        {
            var document = new CoreDocument(itemSource, this, ToId(t), ToName(t), ToType(t));
            document.Store("filename", t.FileName);
            return document;
        }

        public string ToName(Task t)
        {
            return t.Name;
        }

        public string ToType(Task t)
        {
            return t.GetType().Name;
        }
    }    

    [Export(typeof(TaskRepository))]
    [Export(typeof(IItemSource))]
    public class TaskRepository : BaseItemSource
    {
        private string _tasksLocation;
        private Regex _safeFilenameRegex;
        private JsonSerializerSettings _settings;
        private Formatting _formatting;

        [Import]
        public ILog Log { get; set; }

        [ImportConfiguration]
        public CoreConfiguration Configuration { get; set; }

        [ImportingConstructor]
        public TaskRepository()
        {
            _settings = new JsonSerializerSettings();
            _settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            
            _formatting = Formatting.Indented;

            _safeFilenameRegex = new Regex(@"[<>:""/\\|?*]");
        }

        public void CreateTask(Task task)
        {
            task.FileName = TaskToFileName(task);
            string serializedTask = JsonConvert.SerializeObject(task, _formatting, _settings);
            File.WriteAllText(Path.Combine(TasksLocation, task.FileName), serializedTask);
        }

        public void UpdateTask(Task task)
        {
            string serializedTask = JsonConvert.SerializeObject(task, _formatting, _settings);

            File.WriteAllText(Path.Combine(TasksLocation, task.FileName), serializedTask);
        }

        public Task FromFileName(string filename)
        {
            var fullPath = Path.Combine(TasksLocation, filename);
            return File.Exists(fullPath) ? FileToTask(fullPath) : null;
        }

        private string TaskToFileName(Task task)
        {
            int i = 1;
            string rootFileName = _safeFilenameRegex.Replace(task.Name, "_")+".task";
            var fileName = rootFileName;
            while(File.Exists(Path.Combine(TasksLocation, fileName)))
            {
                fileName = rootFileName + "_" + i.ToString();
                i += 1;
            }
            return fileName;
        }

        public override IEnumerable<object> GetItems()
        {
            return Directory.EnumerateFiles(TasksLocation, "*.task")
                .Select(FileToTask)
                .Where(t => t != null);
        }


        private Task FileToTask(string f)
        {
            Task fileToTask = null;
            try
            {
                fileToTask = JsonConvert.DeserializeObject<Task>(File.ReadAllText(f), _settings);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error parsing task file {0}", f);
            }
            return fileToTask;
        }


        public override bool Persistent
        {
            get { return false; }
        }

        public string TasksLocation
        {
            get
            {
                var location = Path.Combine(Configuration.DataDirectory, "Tasks");
                if (location != _tasksLocation)
                {
                    _tasksLocation = location;
                    if (!Directory.Exists(TasksLocation)) Directory.CreateDirectory(_tasksLocation);
                    if (!Directory.Exists(Path.Combine(TasksLocation, "Archive"))) Directory.CreateDirectory(Path.Combine(_tasksLocation, "Archive"));
                }

                return _tasksLocation;
            }
        }

        public void ArchiveTask(Task task)
        {
            File.Move(Path.Combine(TasksLocation, task.FileName),
                      Path.Combine(TasksLocation, "Archive", task.FileName));
        }
    }

    public class Task : IItem
    {
        public class Duration
        {
            public DateTimeOffset Start;
            public DateTimeOffset End;
        }

        public string Name { get; private set; }
        public string FileName { get; set; }

        public List<Duration> Durations { get; set; }
        public DateTimeOffset? LastStartDate { get; set; }

        public Task(string name)
        {
            Name = name;
            Durations = new List<Duration>();
        }

        public void Start()
        {
            LastStartDate = DateTimeOffset.Now;
        }

        public void Stop()
        {
            Durations.Add(new Duration {Start = LastStartDate ?? DateTimeOffset.Now, End = DateTimeOffset.Now});
            LastStartDate = null;
        }

        public string Text
        {
            get { return Name; }
        }

        public string Description
        {
            get
            {
                var totalDuration = new TimeSpan(Durations.Select(d => d.End - d.Start).Sum(t => t.Ticks));
                var description =  string.Format("{0} durations, total {1}h{2}m",
                                     Durations.Count,
                                     totalDuration.Hours, totalDuration.Minutes);
                if (LastStartDate.HasValue)
                {
                    description += string.Format(". last started {0:0.##} minutes ago", (DateTimeOffset.Now - LastStartDate.Value).TotalMinutes);
                }
                return description;
            }
        }

        public object Item
        {
            get { return this; }
        }
    }
}