using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueRepository
    {
        private readonly string m_QueuesPath;
        private readonly JsonSerializerSettings m_JsonSettings;

        internal QueueRepository(string modPath)
        {
            m_QueuesPath = Path.Combine(modPath ?? string.Empty, "Queues");
            m_JsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        internal List<QueueFile> Queues { get; } = new List<QueueFile>();

        internal void LoadAll()
        {
            Queues.Clear();
            Directory.CreateDirectory(m_QueuesPath);

            foreach (string path in Directory.GetFiles(m_QueuesPath, "*.json"))
            {
                try
                {
                    BuffQueueDefinition queue = JsonConvert.DeserializeObject<BuffQueueDefinition>(
                        File.ReadAllText(path),
                        m_JsonSettings);

                    if (queue == null)
                    {
                        continue;
                    }

                    Normalize(queue);
                    Queues.Add(new QueueFile(path, queue));
                }
                catch (Exception ex)
                {
                    Logger.Exception("Failed to load queue file: " + path, ex);
                }
            }

            if (Queues.Count == 0)
            {
                CreateQueue("Daily buffs");
            }
        }

        internal QueueFile CreateQueue(string name)
        {
            Directory.CreateDirectory(m_QueuesPath);

            BuffQueueDefinition queue = new BuffQueueDefinition();
            queue.Name = string.IsNullOrWhiteSpace(name) ? "New queue" : name.Trim();
            Normalize(queue);

            string path = GetUniqueQueuePath(queue.Name);
            QueueFile file = new QueueFile(path, queue);
            Queues.Add(file);
            Save(file);
            return file;
        }

        internal void DeleteQueue(int index)
        {
            if (index < 0 || index >= Queues.Count)
            {
                return;
            }

            QueueFile file = Queues[index];
            Queues.RemoveAt(index);

            try
            {
                if (File.Exists(file.Path))
                {
                    File.Delete(file.Path);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to delete queue file: " + file.Path, ex);
            }

            if (Queues.Count == 0)
            {
                CreateQueue("Daily buffs");
            }
        }

        internal void SaveAll()
        {
            foreach (QueueFile file in Queues)
            {
                Save(file);
            }
        }

        internal void Save(QueueFile file)
        {
            if (file == null || file.Queue == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(m_QueuesPath);
                Normalize(file.Queue);
                File.WriteAllText(file.Path, JsonConvert.SerializeObject(file.Queue, m_JsonSettings));
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to save queue file: " + (file != null ? file.Path : "<null>"), ex);
            }
        }

        internal void Rename(QueueFile file, string name)
        {
            if (file == null || file.Queue == null || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            file.Queue.Name = name.Trim();
            Save(file);
        }

        private static void Normalize(BuffQueueDefinition queue)
        {
            if (string.IsNullOrWhiteSpace(queue.Name))
            {
                queue.Name = "New queue";
            }

            if (queue.Actions == null)
            {
                queue.Actions = new List<BuffQueueAction>();
            }

            foreach (BuffQueueAction action in queue.Actions)
            {
                if (action.Metamagic == null)
                {
                    action.Metamagic = new List<string>();
                }

                if (action.TargetIds == null)
                {
                    action.TargetIds = new List<string>();
                }

                if (action.TargetNames == null)
                {
                    action.TargetNames = new List<string>();
                }
            }
        }

        private string GetUniqueQueuePath(string queueName)
        {
            string baseName = SanitizeFileName(queueName);
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "Queue";
            }

            string path = Path.Combine(m_QueuesPath, baseName + ".json");
            int suffix = 2;
            while (File.Exists(path))
            {
                path = Path.Combine(m_QueuesPath, baseName + "_" + suffix + ".json");
                suffix++;
            }

            return path;
        }

        private static string SanitizeFileName(string name)
        {
            string value = name ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Trim();
        }
    }
}
