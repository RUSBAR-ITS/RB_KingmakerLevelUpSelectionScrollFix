using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueRepository
    {
        private readonly string m_QueuesPath;
        private readonly QueueJsonSerializer m_JsonSerializer;

        internal QueueRepository(string modPath)
        {
            m_QueuesPath = Path.Combine(modPath ?? string.Empty, "Queues");
            m_JsonSerializer = new QueueJsonSerializer();
        }

        internal List<QueueFile> Queues { get; } = new List<QueueFile>();

        internal void LoadAll()
        {
            Queues.Clear();
            Directory.CreateDirectory(m_QueuesPath);

            string[] paths = Directory.GetFiles(m_QueuesPath, "*.json");
            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                try
                {
                    bool containsName;
                    bool containsActions;
                    BuffQueueDefinition queue = m_JsonSerializer.Deserialize(
                        File.ReadAllText(path, Encoding.UTF8),
                        out containsName,
                        out containsActions);

                    if (queue == null)
                    {
                        continue;
                    }

                    if (!containsName || string.IsNullOrWhiteSpace(queue.Name))
                    {
                        queue.Name = Path.GetFileNameWithoutExtension(path);
                        Logger.Warning("Recovered queue name from file name: " + queue.Name + ".");
                    }

                    if (!containsActions)
                    {
                        queue.Actions = new List<BuffQueueAction>();
                        Logger.Warning("Queue file contains no actions field and was recovered as empty: " + path + ".");
                    }

                    Normalize(queue);
                    Queues.Add(new QueueFile(path, queue));
                    Logger.Info("Loaded queue. name=" + queue.Name + ", actions=" + queue.Actions.Count + ".");
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
                string json = m_JsonSerializer.Serialize(file.Queue);
                WriteSafely(file.Path, json);
                Logger.Info("Saved queue. name=" + file.Queue.Name + ", actions=" + file.Queue.Actions.Count + ".");
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

                if (action.CandidateCasters == null)
                {
                    action.CandidateCasters = new List<QueueCasterReference>();
                }

                if (action.CandidateCasters.Count == 0 && !string.IsNullOrEmpty(action.CasterId))
                {
                    action.CandidateCasters.Add(new QueueCasterReference
                    {
                        CasterId = action.CasterId,
                        CasterName = action.CasterName,
                        SpellbookId = action.SpellbookId,
                        SpellbookName = action.SpellbookName
                    });
                }

                if (action.CastTargetIds == null)
                {
                    action.CastTargetIds = new List<string>();
                }

                if (action.CastTargetNames == null)
                {
                    action.CastTargetNames = new List<string>();
                }

                if (action.CastTargetIds.Count == 0 && !string.IsNullOrEmpty(action.CastTargetId))
                {
                    action.CastTargetIds.Add(action.CastTargetId);
                    action.CastTargetNames.Add(action.CastTargetName);
                }

                if (action.RecipientIds == null)
                {
                    action.RecipientIds = new List<string>();
                }

                if (action.RecipientNames == null)
                {
                    action.RecipientNames = new List<string>();
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

        private static void WriteSafely(string path, string content)
        {
            string temporaryPath = path + ".tmp";
            string backupPath = path + ".bak";
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));

            if (!File.Exists(path))
            {
                File.Move(temporaryPath, path);
                return;
            }

            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(temporaryPath, path, backupPath, true);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, backupPath, true);
                File.Copy(temporaryPath, path, true);
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                File.Copy(path, backupPath, true);
                File.Copy(temporaryPath, path, true);
                File.Delete(temporaryPath);
            }
        }
    }
}
