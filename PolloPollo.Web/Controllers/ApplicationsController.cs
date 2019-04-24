﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PolloPollo.Services;
using PolloPollo.Shared.DTO;
using PolloPollo.Shared;
using System;

namespace PolloPollo.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationRepository _applicationRepository;

        public ApplicationsController(IApplicationRepository repo)
        {
            _applicationRepository = repo;
        }

        // GET: api/Applications
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<ApplicationListDTO>> Get(int offset, int amount)
        {
            if (amount == 0)
            {
                amount = int.MaxValue;
            }

            var read = _applicationRepository.ReadOpen();
            var list = await _applicationRepository.ReadOpen().Skip(offset).Take(amount).ToListAsync();

            return new ApplicationListDTO
            {
                Count = read.Count(),
                List = list
            };
        }

        // GET: api/Applications/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<ActionResult<ApplicationDTO>> Get(int id)
        {
            var application = await _applicationRepository.FindAsync(id);

            if (application == null)
            {
                return NotFound();
            }

            return application;
        }

        // GET api/application/receiver
        [ApiConventionMethod(typeof(DefaultApiConventions),
            nameof(DefaultApiConventions.Get))]
        [AllowAnonymous]
        [HttpGet("receiver/{receiverId}")]
        public async Task<ActionResult<IEnumerable<ApplicationDTO>>> GetByReceiver(int receiverId, string status = "All")
        {
            List<ApplicationDTO> applications = null;

            switch (status)
            {
                case nameof(ApplicationStatusEnum.All):
                    applications = await _applicationRepository.Read(receiverId).ToListAsync();
                    break;
                case nameof(ApplicationStatusEnum.Open):
                case nameof(ApplicationStatusEnum.Closed):
                case nameof(ApplicationStatusEnum.Pending):
                    Enum.TryParse(status, true, out ApplicationStatusEnum parsedStatus);
                    applications = await _applicationRepository.Read(receiverId).Where(a => a.Status == parsedStatus).ToListAsync();
                    break;
                default:
                    return BadRequest("Invalid status in parameter");
            }

            return applications;
        }

        // POST: api/Applications
        [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<ActionResult<ApplicationDTO>> Post([FromBody] ApplicationCreateDTO dto)
        {
            var claimRole = User.Claims.First(c => c.Type == ClaimTypes.Role);

            if (!claimRole.Value.Equals(UserRoleEnum.Receiver.ToString()))
            {
                return Unauthorized();
            }

            var created = await _applicationRepository.CreateAsync(dto);

            if (created == null)
            {
                return Conflict();
            }

            return CreatedAtAction(nameof(Get), new {id = created.ApplicationId}, created);

        }

        // DELETE: api/ApiWithActions/5
        [Route("{userId}/{id}")]
        [HttpDelete()]
        public async Task<ActionResult<bool>> Delete(int userId, int id)
        {
            var claimId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier);
            // Identity check of current user
            // if id don't match, it is forbidden to update
            if (!claimId.Value.Equals(userId.ToString()))
            {
                return Forbid();
            }

            // If the application is not open it is forbidden to delete
            var application = await _applicationRepository.FindAsync(id);

            if (application == null) {
                return NotFound();
            }

            if (application.Status != ApplicationStatusEnum.Open) {
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            return await _applicationRepository.DeleteAsync(userId, id); ;
        }
    }
}
