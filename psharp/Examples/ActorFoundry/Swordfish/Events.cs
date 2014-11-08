using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class eLocal : Event { }

    internal class eStop : Event { }

    internal class eCreateAccount : Event
    {
        public eCreateAccount(Object payload)
            : base(payload)
        { }
    }

    internal class eCloseAccount : Event
    {
        public eCloseAccount(Object payload)
            : base(payload)
        { }
    }

    internal class eWithdraw : Event
    {
        public eWithdraw(Object payload)
            : base(payload)
        { }
    }

    internal class eDeposit : Event
    {
        public eDeposit(Object payload)
            : base(payload)
        { }
    }

    internal class eBalanceInquiry : Event
    {
        public eBalanceInquiry(Object payload)
            : base(payload)
        { }
    }

    internal class eUnlock : Event
    {
        public eUnlock(Object payload)
            : base(payload)
        { }
    }

    internal class eLock : Event
    {
        public eLock(Object payload)
            : base(payload)
        { }
    }

    internal class eClose : Event
    {
        public eClose(Object payload)
            : base(payload)
        { }
    }

    internal class eCloseAck : Event
    {
        public eCloseAck(Object payload)
            : base(payload)
        { }
    }

    internal class eTransfer : Event
    {
        public eTransfer(Object payload)
            : base(payload)
        { }
    }

    internal class eTransComplete : Event
    {
        public eTransComplete(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdate : Event
    {
        public eUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eCreateCallback : Event
    {
        public eCreateCallback(Object payload)
            : base(payload)
        { }
    }
}
