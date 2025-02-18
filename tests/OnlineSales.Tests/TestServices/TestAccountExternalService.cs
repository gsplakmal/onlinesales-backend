﻿// <copyright file="TestAccountExternalService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using OnlineSales.DTOs;
using OnlineSales.Interfaces;

namespace OnlineSales.Tests.TestServices
{
    public class TestAccountExternalService : IAccountExternalService
    {
        public Task<AccountDetailsInfo> GetAccountDetails(string domain)
        {
            var account = new AccountDetailsInfo()
            {
                AccountSynced = true,
                City = "Colombo",
                CountryCode = "LK",
                EmployeesRange = "2K - 5K",
                Name = "Wave access Sri Lanka",
                Revenue = 90000000,
                StateCode = "WP",
                SocialMedia = new Dictionary<string, string>()
                {
                    { "Facebook", "https://fb.com/waveaccess" },
                    { "Instagram", "https://www.instagram.com/waveaccess" },
                },

                Tags = new string[] { "Information technology", "App Development" },
            };

            return Task.FromResult(account);
        }
    }
}
