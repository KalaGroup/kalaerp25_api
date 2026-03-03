namespace KalaGenset.ERP.Core.Interface
{
    public interface ICanopy
    {
        //Fetch Canopy Plan Details
        public Task<List<Dictionary<string, object>>> GetCanopyPlanAsync(string strJobCardType, string strcompID);
    }
}
