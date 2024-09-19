using System;
using TypeWrap;

namespace TypeWrapTest
{
    public class MyEvents
    {
        public event Action onEvent;

        public void DoSomething()
        {
            onEvent?.Invoke();
        }
    }

    [WrapType(typeof(MyEvents))]
    public partial class EventWrapper { }
}
