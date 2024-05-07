using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    public class ExponentialMovingAverage
    {
        private double alpha;
        private double average;

        public ExponentialMovingAverage(double alpha)
        {
            this.alpha = alpha;
            average = 0;
        }

        public void Add(double value)
        {
            if (average == 0)
                average = value;
            else
                average = alpha * value + (1 - alpha) * average;
        }

        public double Calculate()
        {
            return average;
        }
    }
    public class DownloadSpeedCalculator
    {
        private ExponentialMovingAverage speedAverage;
        private long lastDownloadedBytes;
        private DateTime lastUpdateTime;

        public DownloadSpeedCalculator(double smoothingFactor = 0.3)
        {
            speedAverage = new ExponentialMovingAverage(smoothingFactor);
            lastDownloadedBytes = 0;
            lastUpdateTime = DateTime.Now;
        }

        public void UpdateSpeed(long currentDownloadedBytes)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan elapsedTime = currentTime - lastUpdateTime;

            if (elapsedTime.TotalSeconds > 0)
            {
                double downloadSpeed = (currentDownloadedBytes - lastDownloadedBytes) / elapsedTime.TotalSeconds;
                speedAverage.Add(downloadSpeed);
                lastDownloadedBytes = currentDownloadedBytes;
                lastUpdateTime = currentTime;
            }
        }

        public double GetCurrentSpeed()
        {
            return speedAverage.Calculate();
        }
    }
}
