using Microsoft.Diagnostics.Runtime;
using System;

namespace HeapViewer
{
    public class ClrMDSession : IDisposable
    {
        public DataTarget Target { get; set; }
        public ClrRuntime Clr { get; set; }

        public ClrHeap ManagedHeap
        {
            get
            {
                if (_managedHeap == null)
                {
                    if (Clr == null)
                        throw new InvalidOperationException("First open a Clr/Target to get a ManagedHeap");

                    _managedHeap = Clr.Heap;
                }
                return _managedHeap;
            }
            set { _managedHeap = value; }
        }
        private ClrHeap _managedHeap;

        string _dumpFilename;

        public bool Open(string dumpFilename)
        {
            Target = null;
            Clr = null;

            Target = DataTarget.LoadDump(dumpFilename);
            ClrInfo clrInfo = Target.ClrVersions[0];
            try
            {
                Clr = clrInfo.CreateRuntime();
            }
            catch (Exception)
            {
                Target = null;
                Clr = null;

                throw;
            }

            if (Clr == null)
            {
                Target = null;
                return false;
            }

            _dumpFilename = dumpFilename;
            return true;
        }

        public void Dispose()
        {
            if (Target == null) return;

            Target.Dispose();
            Target = null;
            Clr = null;
        }
    }
}
