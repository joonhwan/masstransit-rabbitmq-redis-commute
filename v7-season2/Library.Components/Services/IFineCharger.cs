using System;
using System.Threading.Tasks;

namespace Library.Components.Services
{
    public enum ChargeResult
    {
        Charged,
        Overriden
    }
    
    public interface IFineCharger
    {
        Task<ChargeResult> Charge(Guid memberId, decimal fineAmount);
    }
}