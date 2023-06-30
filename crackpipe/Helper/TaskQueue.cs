using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace crackpipe.Helper
{
    public class TaskQueue
    {
        #region Singleton
        private static TaskQueue instance = null;
        private static readonly object padlock = new object();

        public static TaskQueue Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new TaskQueue();
                    }
                    return instance;
                }
            }
        }
        #endregion
        private SemaphoreSlim m_Semaphore;
        private List<int> m_ProcessedIds;
        private int maxProcessCount = 1;

        public TaskQueue()
        {
            m_Semaphore = new SemaphoreSlim(maxProcessCount, maxProcessCount);
            m_ProcessedIds = new List<int>();           
        }
        public async Task Enqueue(Func<Task> taskGenerator, int id)
        {
            m_ProcessedIds.Add(id);
            await m_Semaphore.WaitAsync();
            try
            {
                //Debug.WriteLine($"->Task stated with id: {id}");
                await taskGenerator();
            }
            finally
            {
                m_Semaphore.Release();
                m_ProcessedIds.Remove(id);
                //Debug.WriteLine($"###Task endet with id: {id}");
            }
        }
        public void ClearQueue()
        {
            if (m_Semaphore != null)
            {
                m_Semaphore.Dispose();
            }
            m_Semaphore = new SemaphoreSlim(maxProcessCount, maxProcessCount);
        }
        public bool IsAlreadyInProcess(int id)
        {
            return m_ProcessedIds.Contains(id);
        }
        public async Task WaitForProcessToFinish(int id)
        {
            while (IsAlreadyInProcess(id))
            {
                await Task.Delay(25);
            }
        }
    }
}
