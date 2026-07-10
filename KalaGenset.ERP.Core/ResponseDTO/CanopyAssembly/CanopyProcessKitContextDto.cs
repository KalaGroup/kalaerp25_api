namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Kit context — populates Bal (remaining pieces on the open PSH record)
    // and the PFB rate used for downstream master-insert. Called after the
    // user picks a kit in PSH mode.
    public class CanopyProcessKitContextDto
    {
        public double Bal   { get; set; }
        public double SRate { get; set; }
    }
}
