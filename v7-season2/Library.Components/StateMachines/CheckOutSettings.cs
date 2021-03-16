using System;

namespace Library.Components.StateMachines
{
    public interface CheckOutSettings
    {
        /// <summary>
        /// 책 대출 기간 기본 설정값
        /// </summary>
        TimeSpan DefaultCheckOutDuration { get; }
        
        /// <summary>
        /// 최대 Duration 허용 값
        /// </summary>
        TimeSpan CheckOutDurationLimit { get; }
    }
}