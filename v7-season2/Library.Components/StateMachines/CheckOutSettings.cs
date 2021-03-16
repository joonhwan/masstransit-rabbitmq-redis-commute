using System;

namespace Library.Components.StateMachines
{
    public interface CheckOutSettings
    {
        /// <summary>
        /// 책 대출 기간 기본 설정값
        /// </summary>
        TimeSpan DefaultCheckOutDuration { get; }
    }
}