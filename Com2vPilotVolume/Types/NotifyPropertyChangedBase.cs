using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly Dictionary<string, object?> inner = new Dictionary<string, object?>();

        protected void UpdateProperty<T>(string key, T value)
        {
            inner[key] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
        }

        protected T? GetProperty<T>(string key)
        {
            T? ret;
            if (inner.ContainsKey(key))
            {
                ret = (T?)inner[key];
            }
            else
            {
                ret = default;
            }

            return ret;
        }
    }
}
