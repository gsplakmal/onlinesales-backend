﻿// <copyright file="EmailGroupsController.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;
using OnlineSales.Configuration;
using OnlineSales.Data;
using OnlineSales.DTOs;
using OnlineSales.Entities;

namespace OnlineSales.Controllers;

[Authorize]
[Route("api/[controller]")]
public class EmailGroupsController : BaseController<EmailGroup, EmailGroupCreateDto, EmailGroupUpdateDto, EmailGroupDetailsDto>
{
    public EmailGroupsController(PgDbContext dbContext, IMapper mapper, IOptions<ApiSettingsConfig> apiSettingsConfig, EsDbContext esDbContext)
    : base(dbContext, mapper, apiSettingsConfig, esDbContext)
    {
    }
}