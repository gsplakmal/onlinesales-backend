﻿// <copyright file="IEmailFromTemplateService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using OnlineSales.DTOs;

namespace OnlineSales.Interfaces;

public interface IEmailFromTemplateService
{
    Task SendAsync(string templateName, string recipient, Dictionary<string, string> templateArguments, List<AttachmentDto>? attachments);

    Task SendToCustomerAsync(int customerId, string templateName, Dictionary<string, string> templateArguments, List<AttachmentDto>? attachments, int scheduleId = 0);
}
