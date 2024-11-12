using System;

namespace GCMon
{
    public class GCNotif
    {
        public delegate void GCNotifHandler(object sender, EventArgs e);
        public event GCNotifHandler OnGC;

        ~GCNotif()
        {
            GCNotifHandler handler = OnGC;
            OnGC?.Invoke(this, null);

            // if we re-register for finalization, since we will move to the next gen,
            // we might miss younger ones
            //GC.ReRegisterForFinalize(this);

            var newNotif = new GCNotif();
            newNotif.OnGC += handler;
        }
    }
}
