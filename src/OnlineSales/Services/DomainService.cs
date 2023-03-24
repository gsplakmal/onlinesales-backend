﻿// <copyright file="DomainService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Text;
using DnsClient;
using DnsClient.Protocol;
using HtmlAgilityPack;
using OnlineSales.Entities;
using OnlineSales.Services;

namespace OnlineSales.Interfaces
{
    public class DomainService : IDomainService
    {       
        private readonly LookupClient lookupClient;
        // private readonly IMxVerifyService mxVerifyService;

        public DomainService(IMxVerifyService mxVerifyService)
        {
            // this.mxVerifyService = mxVerifyService;

            lookupClient = new LookupClient(new LookupClientOptions
            {
                UseCache = true,
                Timeout = new TimeSpan(0, 0, 60),                
            });
        }

        public async Task Verify(Domain domain)
        {
            if (domain.DnsCheck == null)
            {
                await VerifyDns(domain);
            }            

            if (domain.DnsCheck is true)
            {
                if (domain.HttpCheck == null)
                {
                    await VerifyHttp(domain);
                }

                // if (domain.MxCheck == null)
                // {
                //     await VerifyMX(domain);
                // }       
            }
            else
            {
                domain.HttpCheck = false;
                domain.MxCheck = false;
                domain.Url = null;
                domain.Title = null;
                domain.Description = null;
            }
        }

        public string GetDomainNameByUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return string.Empty;
                }

                Uri uri = new Uri(url);
                string domain = uri.Host;
                string[] parts = domain.Split('.');
                string domainWithoutSubdomains = parts[parts.Length - 2] + "." + parts[parts.Length - 1];

                return domainWithoutSubdomains;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return string.Empty;
            }
        }

        private async Task VerifyHttp(Domain domain)
        {
            domain.HttpCheck = false;

            var urls = new string[]
            {
            "https://" + domain.Name,
            "https://www." + domain.Name,
            "http://" + domain.Name,
            "http://www." + domain.Name,
            };

            foreach (var url in urls)
            {
                var responce = await GetRequest(url);

                if (responce != null && responce.RequestMessage != null && responce.RequestMessage.RequestUri != null)
                {
                    domain.HttpCheck = true;

                    domain.Url = responce.RequestMessage.RequestUri.ToString();
                    var web = new HtmlWeb();
                    var htmlDoc = await web.LoadFromWebAsync(domain.Url, Encoding.UTF8);

                    if (htmlDoc != null)
                    {
                        domain.Title = GetTitle(htmlDoc);
                        domain.Description = GetDescription(htmlDoc);
                    }

                    break;
                }
            }
        }

        /*
        private async Task VerifyMX(Domain domain)
        {
            domain.MxCheck = false;

            var mxRecords = await lookupClient.QueryAsync(domain.Name, QueryType.MX);

            var orderedMxRecordValues = from r in mxRecords.AllRecords
                                        where r is MxRecord
                                        orderby ((MxRecord)r).Preference ascending
                                        select ((MxRecord)r).Exchange.Value;

            foreach (var mxRecordValue in orderedMxRecordValues)
            {
                var mxVerify = await mxVerifyService.Verify(mxRecordValue);

                if (mxVerify)
                {
                    domain.MxCheck = true;
                    break;
                }
            }
        }
        */

        private async Task VerifyDns(Domain domain)
        {
            domain.DnsRecords = null;
            domain.DnsCheck = false;
                     
            var result = await lookupClient.QueryAsync(domain.Name, QueryType.ANY);

            var dnsRecords = GetDnsRecords(result, domain);

            if (dnsRecords.Count > 0)
            {
                domain.DnsCheck = true;
                domain.DnsRecords = dnsRecords;
            }
        }

        private List<DnsRecord> GetDnsRecords(IDnsQueryResponse dnsQueryResponse, Domain d)
        {
            var dnsRecords = new List<DnsRecord>();

            foreach (var dnsResponseRecord in dnsQueryResponse.AllRecords)
            {
                try
                {
                    var dnsRecord = new DnsRecord
                    {
                        DomainName = dnsResponseRecord.DomainName.Value,
                        RecordClass = dnsResponseRecord.RecordClass.ToString(),
                        RecordType = dnsResponseRecord.RecordType.ToString(),
                        TimeToLive = dnsResponseRecord.TimeToLive,
                    };

                    switch (dnsResponseRecord)
                    {
                        case ARecord a:
                            if (dnsRecord.DomainName != d.Name + ".")
                            {
                                // we are only interesting in an A record for the main domain
                                continue;
                            }

                            dnsRecord.Value = a.Address.ToString();
                            break;
                        case CNameRecord cname:
                            dnsRecord.Value = cname.CanonicalName.Value;
                            break;
                        case MxRecord mx:
                            dnsRecord.Value = mx.Exchange.Value;
                            break;
                        case TxtRecord txt:
                            dnsRecord.Value = string.Concat(txt.Text);
                            break;
                        case NsRecord ns:
                            dnsRecord.Value = ns.NSDName.Value;
                            break;
                        default:
                            continue;
                    }

                    dnsRecords.Add(dnsRecord);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error reading DNS record.");
                }
            }

            return dnsRecords;
        }

        private async Task<HttpResponseMessage?> GetRequest(string url)
        {
            HttpClient client = new HttpClient();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to fetch url: {url}");
                return null;
            }
        }

        private string? GetTitle(HtmlDocument htmlDoc)
        {
            var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

            if (htmlNode != null && !string.IsNullOrEmpty(htmlNode.InnerText))
            {
                return htmlNode.InnerText;
            }

            var title = GetNodeContentByAttr(htmlDoc, "title");

            if (!string.IsNullOrEmpty(title))
            {
                return title;
            }

            htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//h1");

            if (htmlNode != null && !string.IsNullOrEmpty(htmlNode.InnerText))
            {
                return htmlNode.InnerText;
            }

            return null;
        }

        private string? GetDescription(HtmlDocument htmlDoc)
        {
            return GetNodeContentByAttr(htmlDoc, "description");
        }

        private string? GetNodeContentByAttr(HtmlDocument htmlDoc, string value)
        {
            var result = GetNodeContentByAttr(htmlDoc, "name", value);
            if (result == null)
            {
                result = GetNodeContentByAttr(htmlDoc, "property", value);
            }

            return result;
        }

        private string? GetNodeContentByAttr(HtmlDocument htmlDoc, string attrName, string value)
        {
            string? GetNodeContent(HtmlDocument htmlDoc, string attrName, string value)
            {
                var htmlNode = htmlDoc.DocumentNode.SelectSingleNode(string.Format("//meta[@{0}='{1}']", attrName, value));
                if (htmlNode != null && htmlNode.Attributes.Contains("content"))
                {
                    return htmlNode.GetAttributeValue("content", null);
                }

                return null;
            }

            var res = GetNodeContent(htmlDoc, attrName, value);
            if (res == null)
            {
                res = GetNodeContent(htmlDoc, attrName, "og:" + value);
            }

            return res;
        }
    }
}