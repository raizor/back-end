﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolloPollo.Repository;
using PolloPollo.Shared;

namespace PolloPollo.Web.Controllers 
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducersController : ControllerBase
    {
        private readonly IProducerRepository _repository; 

        public ProducersController(IProducerRepository repository)
        {
            _repository = repository; 
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProducerDTO>>> Get()
        {
            return await _repository.Read().ToListAsync();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProducerDTO>> Get(int id)
        {
            var producer = await _repository.FindAsync(id);

            if (producer == null)
            {
                return NotFound();
            }

            return producer;
        }


        // PUT api/values/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] UserUpdateDTO dto)
        {
            var result = await _repository.UpdateAsync(dto);

            if (result)
            {
                return NoContent();
            }

            return NotFound();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                return NoContent();
            }

            return NotFound();
        }
    }
}