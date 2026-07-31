using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request.ControlPanelBox;
using KalaGenset.ERP.Core.ResponseDTO.ControlPanelBox;

namespace KalaGenset.ERP.Core.Interface
{
    /// <summary>
    /// Control Panel Box domain — plan / process / checker flows for the
    /// separate control-panel assembly line (parts starting with "003%").
    /// </summary>
    public interface IControlPanelBox
    {
        /// <summary>
        /// Returns TOP 25 candidate Control Panel Box BOMs whose <c>KVA</c>
        /// column matches the operator-picked value.
        /// One row per unique <c>KitCode</c>, ordered by <c>PartDesc</c>.
        /// </summary>
        /// <param name="kva">
        /// KVA the operator picked in the dropdown (e.g. "250"). Passed to
        /// SQL as-is via a parameter — no interpolation.
        /// </param>
        Task<List<ControlPanelBoxPlanRowDto>> GetPlanRowsByKvaAsync(string kva);

        /// <summary>
        /// Save the operator's manual Control Panel Plan. Step 1 of the
        /// migration inserts only the CanopyPlan header via SP
        /// <c>InsertCanopyPlan_Maker_Checker</c>. Step 2 (CanopyPlanDetails
        /// per Row) is a future add.
        /// </summary>
        Task<SubmitControlPanelBoxPlanResponse> SubmitPlanAsync(
            SubmitControlPanelBoxPlanRequest request);
    }
}
