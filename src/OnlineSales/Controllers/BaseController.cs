﻿// <copyright file="BaseController.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Web;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nest;
using OnlineSales.Configuration;
using OnlineSales.Data;
using OnlineSales.DataAnnotations;
using OnlineSales.Entities;
using OnlineSales.Infrastructure;

namespace OnlineSales.Controllers
{
    public class BaseController<T, TC, TU, TD> : ControllerBase
        where T : BaseEntityWithId, new()
        where TC : class
        where TU : class
        where TD : class
    {
        protected readonly DbSet<T> dbSet;
        protected readonly PgDbContext dbContext;
        protected readonly IMapper mapper;
        private readonly IOptions<ApiSettingsConfig> apiSettingsConfig;
        private readonly ElasticClient elasticClient;

        public BaseController(PgDbContext dbContext, IMapper mapper, IOptions<ApiSettingsConfig> apiSettingsConfig, EsDbContext esDbContext)
        {
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<T>();
            this.mapper = mapper;
            this.apiSettingsConfig = apiSettingsConfig;
            this.elasticClient = esDbContext.ElasticClient;
        }

        // GET api/{entity}s/5
        [HttpGet("{id}")]
        // [EnableQuery]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public virtual async Task<ActionResult<TD>> GetOne(int id)
        {
            var result = await FindOrThrowNotFound(id);

            var resultConverted = mapper.Map<TD>(result);

            return Ok(resultConverted);
        }

        // POST api/{entity}s
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public virtual async Task<ActionResult<TD>> Post([FromBody] TC value)
        {
            var newValue = mapper.Map<T>(value);
            var result = await dbSet.AddAsync(newValue);
            await dbContext.SaveChangesAsync();

            var resultsToClient = mapper.Map<TD>(newValue);

            return CreatedAtAction(nameof(GetOne), new { id = result.Entity.Id }, resultsToClient);
        }

        // PUT api/{entity}s/5
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public virtual async Task<ActionResult<TD>> Patch(int id, [FromBody] TU value)
        {
            var existingEntity = await FindOrThrowNotFound(id);

            mapper.Map(value, existingEntity);
            await dbContext.SaveChangesAsync();

            var resultsToClient = mapper.Map<TD>(existingEntity);

            return Ok(resultsToClient);
        }

        // DELETE api/{entity}s/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public virtual async Task<ActionResult> Delete(int id)
        {
            var existingEntity = await FindOrThrowNotFound(id);

            dbContext.Remove(existingEntity);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public virtual async Task<ActionResult<List<TD>>> Get([FromQuery] string? query, bool downloadCsv = false)
        {
            int limit = apiSettingsConfig.Value.MaxListSize;
            var queryCommands = this.Request.QueryString.HasValue ? HttpUtility.UrlDecode(this.Request.QueryString.ToString()).Substring(1).Split('&').ToArray() : new string[0];
            var parseData = new QueryParseData<T>(queryCommands, limit);
            IQueryProvider<T> qp;
            if (typeof(T).GetCustomAttributes(typeof(SupportsElasticAttribute), true).Any())
            {
                var indexPrefix = dbContext.Configuration.GetSection("Elastic:IndexPrefix").Get<string>();
                qp = new ESQueryProvider<T>(elasticClient, parseData, indexPrefix!);
            }
            else
            {
                qp = new DBQueryProvider<T>(this.dbSet!.AsQueryable<T>(), mapper, parseData);
            }

            var result = await qp.GetResult();
            var totalCount = result.Item2;
            this.Response.Headers.Add(ResponseHeaderNames.TotalCount, totalCount.ToString());
            this.Response.Headers.Add(ResponseHeaderNames.AccessControlExposeHeader, ResponseHeaderNames.TotalCount);

            if (downloadCsv)
            {
                this.Response.Headers.Add(ResponseHeaderNames.ContentType, "text/csv");
                this.Response.Headers.Add(ResponseHeaderNames.ContentDisposition, "attachment;filename=data.csv");
            }
           
            return Ok(mapper.Map<List<TD>>(result.Item1));
        }

        protected async Task<T> FindOrThrowNotFound(int id)
        {
            var existingEntity = await (from p in this.dbSet
                                        where p.Id == id
                                        select p).FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                throw new EntityNotFoundException(typeof(T).Name, id.ToString());
            }

            return existingEntity;
        }
    }
}