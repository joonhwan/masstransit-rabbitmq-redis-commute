﻿using System;
using System.Threading.Tasks;

namespace Library.Components.Services
{
    public interface IMemberRegistry
    {
        Task<bool> IsMemberValid(Guid memberId);
    }
}