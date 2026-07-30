using MainClient.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Models
{
    public class DevClientItem
    {
        public int ANDROID_Count;
        public int ANDROID_CLICK_Count;
        public int ANDROID_UV_Count;
        public int iOS_Count;
        public int iOS_CLICK_Count;
        public int iOS_UV_Count;
        public int IncrementAndroidCount()
        {
            return Interlocked.Increment(ref ANDROID_Count);
        }

        public int DecrementAndroidCount()
        {
            return Interlocked.Decrement(ref ANDROID_Count);
        }

        public int IncrementAndroidClickCount()
        {
            return Interlocked.Increment(ref ANDROID_CLICK_Count);
        }

        public int IncrementAndroidUVCount()
        {
            return Interlocked.Increment(ref ANDROID_UV_Count);
        }
        public int DecrementAndroidUVCount()
        {
            return Interlocked.Decrement(ref ANDROID_UV_Count);
        }

        public int IncrementiOSCount()
        {
            return Interlocked.Increment(ref iOS_Count);
        }
        public int DecrementiOSCount()
        {
            return Interlocked.Decrement(ref iOS_Count);
        }
        public int IncrementiOSClickCount()
        {
            return Interlocked.Increment(ref iOS_CLICK_Count);
        }
        public int IncrementiOSUVCount()
        {
            return Interlocked.Increment(ref iOS_UV_Count);
        }
        public int DecrementiOSUVCount()
        {
            return Interlocked.Decrement(ref iOS_UV_Count);
        }

    }
}
