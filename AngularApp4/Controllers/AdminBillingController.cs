using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/admin/billing")]
[Authorize(Policy = "AdminOnly")]
public class AdminBillingController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminBillingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("charges")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingChargeDefinitionDto>>>> GetChargeDefinitions()
    {
        var items = await _db.BillingChargeDefinitions
            .AsNoTracking()
            .OrderBy(x => x.ChargeType)
            .ThenBy(x => x.Name)
            .Select(x => MapChargeDefinition(x))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<BillingChargeDefinitionDto>>.Ok(items));
    }

    [HttpPost("charges")]
    public async Task<ActionResult<ApiResponse<BillingChargeDefinitionDto>>> CreateChargeDefinition([FromBody] SaveBillingChargeDefinitionDto dto)
    {
        var code = Normalize(dto.Code)?.ToUpperInvariant();
        var name = Normalize(dto.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<BillingChargeDefinitionDto>.Fail("Charge code and name are required"));
        }

        if (await _db.BillingChargeDefinitions.AnyAsync(x => x.Code == code))
        {
            return BadRequest(ApiResponse<BillingChargeDefinitionDto>.Fail("Charge code already exists"));
        }

        var item = new BillingChargeDefinition
        {
            ChargeType = ParseEnum(dto.ChargeType, BillingChargeType.Consultation),
            Name = name,
            Code = code,
            Description = Normalize(dto.Description),
            UnitLabel = Normalize(dto.UnitLabel) ?? "unit",
            DefaultAmount = Math.Max(dto.DefaultAmount, 0m),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.BillingChargeDefinitions.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingChargeDefinitionDto>.Ok(MapChargeDefinition(item), "Charge definition created"));
    }

    [HttpPut("charges/{chargeDefinitionId:long}")]
    public async Task<ActionResult<ApiResponse<BillingChargeDefinitionDto>>> UpdateChargeDefinition(long chargeDefinitionId, [FromBody] SaveBillingChargeDefinitionDto dto)
    {
        var item = await _db.BillingChargeDefinitions.FirstOrDefaultAsync(x => x.BillingChargeDefinitionId == chargeDefinitionId);
        if (item is null)
        {
            return NotFound(ApiResponse<BillingChargeDefinitionDto>.Fail("Charge definition not found"));
        }

        var code = Normalize(dto.Code)?.ToUpperInvariant();
        var name = Normalize(dto.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<BillingChargeDefinitionDto>.Fail("Charge code and name are required"));
        }

        if (await _db.BillingChargeDefinitions.AnyAsync(x => x.BillingChargeDefinitionId != chargeDefinitionId && x.Code == code))
        {
            return BadRequest(ApiResponse<BillingChargeDefinitionDto>.Fail("Charge code already exists"));
        }

        item.ChargeType = ParseEnum(dto.ChargeType, item.ChargeType);
        item.Name = name;
        item.Code = code;
        item.Description = Normalize(dto.Description);
        item.UnitLabel = Normalize(dto.UnitLabel) ?? "unit";
        item.DefaultAmount = Math.Max(dto.DefaultAmount, 0m);
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingChargeDefinitionDto>.Ok(MapChargeDefinition(item), "Charge definition updated"));
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingPaymentMethodDto>>>> GetPaymentMethods()
    {
        var items = await _db.BillingPaymentMethods
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => MapPaymentMethod(x))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<BillingPaymentMethodDto>>.Ok(items));
    }

    [HttpPost("payment-methods")]
    public async Task<ActionResult<ApiResponse<BillingPaymentMethodDto>>> CreatePaymentMethod([FromBody] SaveBillingPaymentMethodDto dto)
    {
        var name = Normalize(dto.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<BillingPaymentMethodDto>.Fail("Payment method name is required"));
        }

        if (await _db.BillingPaymentMethods.AnyAsync(x => x.Name == name))
        {
            return BadRequest(ApiResponse<BillingPaymentMethodDto>.Fail("Payment method already exists"));
        }

        var item = new BillingPaymentMethod
        {
            Name = name,
            MethodType = ParseEnum(dto.MethodType, PaymentMethodType.Cash),
            ProviderName = Normalize(dto.ProviderName),
            RequiresReference = dto.RequiresReference,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.BillingPaymentMethods.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingPaymentMethodDto>.Ok(MapPaymentMethod(item), "Payment method created"));
    }

    [HttpPut("payment-methods/{billingPaymentMethodId:long}")]
    public async Task<ActionResult<ApiResponse<BillingPaymentMethodDto>>> UpdatePaymentMethod(long billingPaymentMethodId, [FromBody] SaveBillingPaymentMethodDto dto)
    {
        var item = await _db.BillingPaymentMethods.FirstOrDefaultAsync(x => x.BillingPaymentMethodId == billingPaymentMethodId);
        if (item is null)
        {
            return NotFound(ApiResponse<BillingPaymentMethodDto>.Fail("Payment method not found"));
        }

        var name = Normalize(dto.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<BillingPaymentMethodDto>.Fail("Payment method name is required"));
        }

        if (await _db.BillingPaymentMethods.AnyAsync(x => x.BillingPaymentMethodId != billingPaymentMethodId && x.Name == name))
        {
            return BadRequest(ApiResponse<BillingPaymentMethodDto>.Fail("Payment method already exists"));
        }

        item.Name = name;
        item.MethodType = ParseEnum(dto.MethodType, item.MethodType);
        item.ProviderName = Normalize(dto.ProviderName);
        item.RequiresReference = dto.RequiresReference;
        item.SortOrder = dto.SortOrder;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingPaymentMethodDto>.Ok(MapPaymentMethod(item), "Payment method updated"));
    }

    [HttpGet("partners")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingPartnerDto>>>> GetPartners([FromQuery] string? kind = null)
    {
        var items = await _db.BillingPartners
            .AsNoTracking()
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Name)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(kind))
        {
            var filterKind = ParseEnum(kind, BillingPartnerKind.InsuranceCompany);
            items = items.Where(x => x.Kind == filterKind).ToList();
        }

        return Ok(ApiResponse<IEnumerable<BillingPartnerDto>>.Ok(items.Select(MapPartner).ToList()));
    }

    [HttpPost("partners")]
    public async Task<ActionResult<ApiResponse<BillingPartnerDto>>> CreatePartner([FromBody] SaveBillingPartnerDto dto)
    {
        var kind = ParseEnum(dto.Kind, BillingPartnerKind.InsuranceCompany);
        var name = Normalize(dto.Name);
        var code = Normalize(dto.Code)?.ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(ApiResponse<BillingPartnerDto>.Fail("Partner name and code are required"));
        }

        var exists = await _db.BillingPartners.AnyAsync(x => x.Kind == kind && (x.Name == name || x.Code == code));
        if (exists)
        {
            return BadRequest(ApiResponse<BillingPartnerDto>.Fail("Partner name or code already exists"));
        }

        var item = new BillingPartner
        {
            Kind = kind,
            Name = name,
            Code = code,
            ContactPerson = Normalize(dto.ContactPerson),
            ContactEmail = Normalize(dto.ContactEmail),
            ContactPhone = Normalize(dto.ContactPhone),
            CreditLimit = Math.Max(dto.CreditLimit, 0m),
            ClaimSubmissionMode = Normalize(dto.ClaimSubmissionMode) ?? "Manual",
            Notes = Normalize(dto.Notes),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.BillingPartners.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingPartnerDto>.Ok(MapPartner(item), "Billing partner created"));
    }

    [HttpPut("partners/{billingPartnerId:long}")]
    public async Task<ActionResult<ApiResponse<BillingPartnerDto>>> UpdatePartner(long billingPartnerId, [FromBody] SaveBillingPartnerDto dto)
    {
        var item = await _db.BillingPartners.FirstOrDefaultAsync(x => x.BillingPartnerId == billingPartnerId);
        if (item is null)
        {
            return NotFound(ApiResponse<BillingPartnerDto>.Fail("Billing partner not found"));
        }

        var kind = ParseEnum(dto.Kind, item.Kind);
        var name = Normalize(dto.Name);
        var code = Normalize(dto.Code)?.ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(ApiResponse<BillingPartnerDto>.Fail("Partner name and code are required"));
        }

        var exists = await _db.BillingPartners.AnyAsync(x =>
            x.BillingPartnerId != billingPartnerId &&
            x.Kind == kind &&
            (x.Name == name || x.Code == code));
        if (exists)
        {
            return BadRequest(ApiResponse<BillingPartnerDto>.Fail("Partner name or code already exists"));
        }

        item.Kind = kind;
        item.Name = name;
        item.Code = code;
        item.ContactPerson = Normalize(dto.ContactPerson);
        item.ContactEmail = Normalize(dto.ContactEmail);
        item.ContactPhone = Normalize(dto.ContactPhone);
        item.CreditLimit = Math.Max(dto.CreditLimit, 0m);
        item.ClaimSubmissionMode = Normalize(dto.ClaimSubmissionMode) ?? "Manual";
        item.Notes = Normalize(dto.Notes);
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingPartnerDto>.Ok(MapPartner(item), "Billing partner updated"));
    }

    [HttpGet("rules")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingRuleDto>>>> GetBillingRules()
    {
        var partners = await _db.BillingPartners.AsNoTracking().ToDictionaryAsync(x => x.BillingPartnerId);
        var items = await _db.BillingRules
            .AsNoTracking()
            .OrderBy(x => x.RuleName)
            .ToListAsync();

        var payload = items
            .Where(x => partners.ContainsKey(x.BillingPartnerId))
            .Select(x => MapBillingRule(x, partners[x.BillingPartnerId]))
            .ToList();

        return Ok(ApiResponse<IEnumerable<BillingRuleDto>>.Ok(payload));
    }

    [HttpPost("rules")]
    public async Task<ActionResult<ApiResponse<BillingRuleDto>>> CreateBillingRule([FromBody] SaveBillingRuleDto dto)
    {
        var partner = await _db.BillingPartners.FirstOrDefaultAsync(x => x.BillingPartnerId == dto.BillingPartnerId);
        if (partner is null)
        {
            return BadRequest(ApiResponse<BillingRuleDto>.Fail("Billing partner not found"));
        }

        var name = Normalize(dto.RuleName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<BillingRuleDto>.Fail("Rule name is required"));
        }

        if (await _db.BillingRules.AnyAsync(x => x.BillingPartnerId == dto.BillingPartnerId && x.RuleName == name))
        {
            return BadRequest(ApiResponse<BillingRuleDto>.Fail("A billing rule with this name already exists for the selected partner"));
        }

        var item = new BillingRule
        {
            BillingPartnerId = dto.BillingPartnerId,
            RuleName = name,
            PolicyName = Normalize(dto.PolicyName),
            DiscountPercentage = ClampPercentage(dto.DiscountPercentage),
            CoPayPercentage = ClampPercentage(dto.CoPayPercentage),
            CreditLimit = Math.Max(dto.CreditLimit, 0m),
            ClaimSubmissionWindowDays = Math.Max(dto.ClaimSubmissionWindowDays, 0),
            RequiresPreApproval = dto.RequiresPreApproval,
            Notes = Normalize(dto.Notes),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.BillingRules.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingRuleDto>.Ok(MapBillingRule(item, partner), "Billing rule created"));
    }

    [HttpPut("rules/{billingRuleId:long}")]
    public async Task<ActionResult<ApiResponse<BillingRuleDto>>> UpdateBillingRule(long billingRuleId, [FromBody] SaveBillingRuleDto dto)
    {
        var item = await _db.BillingRules.FirstOrDefaultAsync(x => x.BillingRuleId == billingRuleId);
        if (item is null)
        {
            return NotFound(ApiResponse<BillingRuleDto>.Fail("Billing rule not found"));
        }

        var partner = await _db.BillingPartners.FirstOrDefaultAsync(x => x.BillingPartnerId == dto.BillingPartnerId);
        if (partner is null)
        {
            return BadRequest(ApiResponse<BillingRuleDto>.Fail("Billing partner not found"));
        }

        var name = Normalize(dto.RuleName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<BillingRuleDto>.Fail("Rule name is required"));
        }

        if (await _db.BillingRules.AnyAsync(x => x.BillingRuleId != billingRuleId && x.BillingPartnerId == dto.BillingPartnerId && x.RuleName == name))
        {
            return BadRequest(ApiResponse<BillingRuleDto>.Fail("A billing rule with this name already exists for the selected partner"));
        }

        item.BillingPartnerId = dto.BillingPartnerId;
        item.RuleName = name;
        item.PolicyName = Normalize(dto.PolicyName);
        item.DiscountPercentage = ClampPercentage(dto.DiscountPercentage);
        item.CoPayPercentage = ClampPercentage(dto.CoPayPercentage);
        item.CreditLimit = Math.Max(dto.CreditLimit, 0m);
        item.ClaimSubmissionWindowDays = Math.Max(dto.ClaimSubmissionWindowDays, 0);
        item.RequiresPreApproval = dto.RequiresPreApproval;
        item.Notes = Normalize(dto.Notes);
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<BillingRuleDto>.Ok(MapBillingRule(item, partner), "Billing rule updated"));
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingInvoiceDto>>>> GetInvoices()
    {
        var payload = await BuildInvoiceDtosAsync(includeDetails: true);
        return Ok(ApiResponse<IEnumerable<BillingInvoiceDto>>.Ok(payload));
    }

    [HttpGet("invoices/{billingInvoiceId:long}")]
    public async Task<ActionResult<ApiResponse<BillingInvoiceDto>>> GetInvoice(long billingInvoiceId)
    {
        var payload = await BuildInvoiceDtoAsync(billingInvoiceId, includeDetails: true);
        return payload is null
            ? NotFound(ApiResponse<BillingInvoiceDto>.Fail("Invoice not found"))
            : Ok(ApiResponse<BillingInvoiceDto>.Ok(payload));
    }

    [HttpPost("invoices")]
    public async Task<ActionResult<ApiResponse<BillingInvoiceDto>>> CreateInvoice([FromBody] SaveBillingInvoiceDto dto)
    {
        if (dto.Items is null || !dto.Items.Any())
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("At least one bill item is required"));
        }

        var invoice = new BillingInvoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CreatedAt = DateTime.UtcNow
        };

        var validation = await ApplyInvoiceFieldsAsync(invoice, dto);
        if (validation is not null)
        {
            return validation;
        }

        _db.BillingInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        var itemError = await ReplaceInvoiceItemsAsync(invoice.BillingInvoiceId, dto.Items);
        if (itemError is not null)
        {
            return itemError;
        }

        await RecalculateInvoiceAsync(invoice.BillingInvoiceId);

        var payload = await BuildInvoiceDtoAsync(invoice.BillingInvoiceId, includeDetails: true);
        return Ok(ApiResponse<BillingInvoiceDto>.Ok(payload!, "Invoice created"));
    }

    [HttpPut("invoices/{billingInvoiceId:long}")]
    public async Task<ActionResult<ApiResponse<BillingInvoiceDto>>> UpdateInvoice(long billingInvoiceId, [FromBody] SaveBillingInvoiceDto dto)
    {
        var invoice = await _db.BillingInvoices.FirstOrDefaultAsync(x => x.BillingInvoiceId == billingInvoiceId);
        if (invoice is null)
        {
            return NotFound(ApiResponse<BillingInvoiceDto>.Fail("Invoice not found"));
        }

        if (dto.Items is null || !dto.Items.Any())
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("At least one bill item is required"));
        }

        var validation = await ApplyInvoiceFieldsAsync(invoice, dto);
        if (validation is not null)
        {
            return validation;
        }

        var itemError = await ReplaceInvoiceItemsAsync(invoice.BillingInvoiceId, dto.Items);
        if (itemError is not null)
        {
            return itemError;
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await RecalculateInvoiceAsync(invoice.BillingInvoiceId);

        var payload = await BuildInvoiceDtoAsync(invoice.BillingInvoiceId, includeDetails: true);
        return Ok(ApiResponse<BillingInvoiceDto>.Ok(payload!, "Invoice updated"));
    }

    [HttpPost("invoices/{billingInvoiceId:long}/discount")]
    public async Task<ActionResult<ApiResponse<BillingInvoiceDto>>> ApproveDiscount(long billingInvoiceId, [FromBody] ApproveBillingDiscountDto dto)
    {
        var invoice = await _db.BillingInvoices.FirstOrDefaultAsync(x => x.BillingInvoiceId == billingInvoiceId);
        if (invoice is null)
        {
            return NotFound(ApiResponse<BillingInvoiceDto>.Fail("Invoice not found"));
        }

        var requested = Math.Max(dto.RequestedDiscountAmount, 0m);
        var approved = Math.Max(dto.ApprovedDiscountAmount, 0m);

        if (approved > requested)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Approved discount cannot exceed requested discount"));
        }

        invoice.RequestedDiscountAmount = requested;
        invoice.ApprovedDiscountAmount = approved;
        invoice.DiscountNotes = Normalize(dto.DiscountNotes);
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await RecalculateInvoiceAsync(billingInvoiceId);

        var payload = await BuildInvoiceDtoAsync(billingInvoiceId, includeDetails: true);
        return Ok(ApiResponse<BillingInvoiceDto>.Ok(payload!, "Discount approval updated"));
    }

    [HttpPost("invoices/{billingInvoiceId:long}/payments")]
    public async Task<ActionResult<ApiResponse<BillingInvoiceDto>>> RecordPayment(long billingInvoiceId, [FromBody] RecordBillingPaymentDto dto)
    {
        var invoice = await _db.BillingInvoices.FirstOrDefaultAsync(x => x.BillingInvoiceId == billingInvoiceId);
        if (invoice is null)
        {
            return NotFound(ApiResponse<BillingInvoiceDto>.Fail("Invoice not found"));
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Payment amount must be greater than zero"));
        }

        var paymentMethod = await _db.BillingPaymentMethods.FirstOrDefaultAsync(x => x.BillingPaymentMethodId == dto.BillingPaymentMethodId && x.IsActive);
        if (paymentMethod is null)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Payment method not found"));
        }

        if (paymentMethod.RequiresReference && string.IsNullOrWhiteSpace(dto.ReferenceNumber))
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Reference number is required for the selected payment method"));
        }

        _db.BillingInvoicePayments.Add(new BillingInvoicePayment
        {
            BillingInvoiceId = billingInvoiceId,
            BillingPaymentMethodId = dto.BillingPaymentMethodId,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate == default ? DateTime.UtcNow : dto.PaymentDate,
            ReferenceNumber = Normalize(dto.ReferenceNumber),
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await RecalculateInvoiceAsync(billingInvoiceId);

        var payload = await BuildInvoiceDtoAsync(billingInvoiceId, includeDetails: true);
        return Ok(ApiResponse<BillingInvoiceDto>.Ok(payload!, "Payment recorded"));
    }

    [HttpPost("invoices/{billingInvoiceId:long}/refunds")]
    public async Task<ActionResult<ApiResponse<BillingInvoiceDto>>> ProcessRefund(long billingInvoiceId, [FromBody] ProcessBillingRefundDto dto)
    {
        var invoice = await _db.BillingInvoices.FirstOrDefaultAsync(x => x.BillingInvoiceId == billingInvoiceId);
        if (invoice is null)
        {
            return NotFound(ApiResponse<BillingInvoiceDto>.Fail("Invoice not found"));
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Refund amount must be greater than zero"));
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Refund reason is required"));
        }

        BillingPaymentMethod? paymentMethod = null;
        if (dto.BillingPaymentMethodId.HasValue)
        {
            paymentMethod = await _db.BillingPaymentMethods.FirstOrDefaultAsync(x => x.BillingPaymentMethodId == dto.BillingPaymentMethodId.Value && x.IsActive);
            if (paymentMethod is null)
            {
                return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Refund payment method not found"));
            }
        }

        var status = ParseEnum(dto.Status, RefundStatus.Processed);
        if (status == RefundStatus.Processed && dto.Amount > invoice.AmountPaid)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Processed refund cannot exceed the amount paid"));
        }

        _db.BillingRefunds.Add(new BillingRefund
        {
            BillingInvoiceId = billingInvoiceId,
            BillingPaymentMethodId = dto.BillingPaymentMethodId,
            Amount = dto.Amount,
            Status = status,
            Reason = dto.Reason.Trim(),
            Notes = Normalize(dto.Notes),
            RequestedAt = DateTime.UtcNow,
            ProcessedAt = status == RefundStatus.Processed ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await RecalculateInvoiceAsync(billingInvoiceId);

        var payload = await BuildInvoiceDtoAsync(billingInvoiceId, includeDetails: true);
        return Ok(ApiResponse<BillingInvoiceDto>.Ok(payload!, "Refund recorded"));
    }

    private async Task<ActionResult<ApiResponse<BillingInvoiceDto>>?> ApplyInvoiceFieldsAsync(BillingInvoice invoice, SaveBillingInvoiceDto dto)
    {
        if (!dto.PatientId.HasValue)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Patient is required"));
        }

        if (!dto.BranchId.HasValue)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Branch is required"));
        }

        if (!await _db.Patients.AnyAsync(x => x.PatientId == dto.PatientId.Value))
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Patient not found"));
        }

        if (!await _db.Branches.AnyAsync(x => x.BranchId == dto.BranchId.Value))
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Branch not found"));
        }

        if (dto.AppointmentId.HasValue && !await _db.Appointments.AnyAsync(x => x.AppointmentId == dto.AppointmentId.Value))
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Appointment not found"));
        }

        var payerType = ParseEnum(dto.PayerType, InvoicePayerType.SelfPay);
        BillingPartner? partner = null;
        BillingRule? rule = null;

        if (dto.BillingPartnerId.HasValue)
        {
            partner = await _db.BillingPartners.FirstOrDefaultAsync(x => x.BillingPartnerId == dto.BillingPartnerId.Value && x.IsActive);
            if (partner is null)
            {
                return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Billing partner not found"));
            }
        }

        if (dto.BillingRuleId.HasValue)
        {
            rule = await _db.BillingRules.FirstOrDefaultAsync(x => x.BillingRuleId == dto.BillingRuleId.Value && x.IsActive);
            if (rule is null)
            {
                return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Billing rule not found"));
            }

            if (partner is not null && rule.BillingPartnerId != partner.BillingPartnerId)
            {
                return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Selected billing rule does not belong to the selected billing partner"));
            }

            partner ??= await _db.BillingPartners.FirstOrDefaultAsync(x => x.BillingPartnerId == rule.BillingPartnerId && x.IsActive);
        }

        if (payerType == InvoicePayerType.SelfPay)
        {
            partner = null;
            rule = null;
        }
        else if (partner is null)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("A billing partner is required for insurance and corporate invoices"));
        }

        if (partner is not null &&
            ((payerType == InvoicePayerType.Insurance && partner.Kind != BillingPartnerKind.InsuranceCompany) ||
             (payerType == InvoicePayerType.Corporate && partner.Kind != BillingPartnerKind.PanelOrganization)))
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Selected billing partner does not match the invoice payer type"));
        }

        invoice.PatientId = dto.PatientId;
        invoice.AppointmentId = dto.AppointmentId;
        invoice.BranchId = dto.BranchId;
        invoice.PayerType = payerType;
        invoice.BillingPartnerId = partner?.BillingPartnerId;
        invoice.BillingRuleId = rule?.BillingRuleId;
        invoice.InvoiceDate = dto.InvoiceDate == default ? DateTime.Today : dto.InvoiceDate.Date;
        invoice.DueDate = dto.DueDate?.Date ?? (rule?.ClaimSubmissionWindowDays > 0 ? invoice.InvoiceDate.AddDays(rule.ClaimSubmissionWindowDays) : null);
        invoice.RequestedDiscountAmount = Math.Max(dto.RequestedDiscountAmount, 0m);
        invoice.DiscountNotes = Normalize(dto.DiscountNotes);
        invoice.Notes = Normalize(dto.Notes);
        invoice.ClaimReferenceNumber = Normalize(dto.ClaimReferenceNumber);
        invoice.ClaimStatus = payerType == InvoicePayerType.SelfPay ? BillingClaimStatus.None : ParseEnum(dto.ClaimStatus, BillingClaimStatus.Draft);
        invoice.ClaimSubmittedAt = invoice.ClaimStatus is BillingClaimStatus.Submitted or BillingClaimStatus.UnderReview or BillingClaimStatus.Approved or BillingClaimStatus.Settled
            ? invoice.ClaimSubmittedAt ?? DateTime.UtcNow
            : null;
        invoice.ClaimSettledAt = invoice.ClaimStatus == BillingClaimStatus.Settled ? DateTime.UtcNow : null;

        return null;
    }

    private async Task<ActionResult<ApiResponse<BillingInvoiceDto>>?> ReplaceInvoiceItemsAsync(long billingInvoiceId, IEnumerable<SaveBillingInvoiceItemDto> items)
    {
        var rows = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Description) || x.BillingChargeDefinitionId.HasValue)
            .ToList();

        if (rows.Count == 0)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("At least one valid bill item is required"));
        }

        var chargeDefinitionIds = rows
            .Where(x => x.BillingChargeDefinitionId.HasValue)
            .Select(x => x.BillingChargeDefinitionId!.Value)
            .Distinct()
            .ToList();

        var chargeDefinitions = await _db.BillingChargeDefinitions
            .Where(x => chargeDefinitionIds.Contains(x.BillingChargeDefinitionId))
            .ToDictionaryAsync(x => x.BillingChargeDefinitionId);

        if (chargeDefinitions.Count != chargeDefinitionIds.Count)
        {
            return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("One or more charge definitions could not be found"));
        }

        var existing = await _db.BillingInvoiceItems.Where(x => x.BillingInvoiceId == billingInvoiceId).ToListAsync();
        if (existing.Count != 0)
        {
            _db.BillingInvoiceItems.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        foreach (var row in rows)
        {
            chargeDefinitions.TryGetValue(row.BillingChargeDefinitionId ?? 0, out var chargeDefinition);
            var description = Normalize(row.Description) ?? chargeDefinition?.Name;
            if (string.IsNullOrWhiteSpace(description))
            {
                return BadRequest(ApiResponse<BillingInvoiceDto>.Fail("Bill item description is required"));
            }

            var quantity = row.Quantity <= 0 ? 1m : row.Quantity;
            var unitPrice = row.UnitPrice > 0 ? row.UnitPrice : chargeDefinition?.DefaultAmount ?? 0m;
            var discountAmount = Math.Max(row.DiscountAmount, 0m);
            var totalAmount = Math.Max(quantity * unitPrice - discountAmount, 0m);

            _db.BillingInvoiceItems.Add(new BillingInvoiceItem
            {
                BillingInvoiceId = billingInvoiceId,
                BillingChargeDefinitionId = row.BillingChargeDefinitionId,
                ChargeType = chargeDefinition?.ChargeType ?? ParseEnum(row.ChargeType, BillingChargeType.Consultation),
                Description = description,
                Quantity = quantity,
                UnitPrice = unitPrice,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                Notes = Normalize(row.Notes),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return null;
    }

    private async Task RecalculateInvoiceAsync(long billingInvoiceId)
    {
        var invoice = await _db.BillingInvoices.FirstAsync(x => x.BillingInvoiceId == billingInvoiceId);

        var itemTotal = await _db.BillingInvoiceItems
            .Where(x => x.BillingInvoiceId == billingInvoiceId)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

        var paymentTotal = await _db.BillingInvoicePayments
            .Where(x => x.BillingInvoiceId == billingInvoiceId)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var refundTotal = await _db.BillingRefunds
            .Where(x => x.BillingInvoiceId == billingInvoiceId && x.Status == RefundStatus.Processed)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var lastPayment = await _db.BillingInvoicePayments
            .Where(x => x.BillingInvoiceId == billingInvoiceId)
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => (DateTime?)x.PaymentDate)
            .FirstOrDefaultAsync();

        invoice.TotalAmount = itemTotal;
        invoice.RequestedDiscountAmount = Math.Max(invoice.RequestedDiscountAmount, 0m);
        invoice.ApprovedDiscountAmount = Math.Clamp(invoice.ApprovedDiscountAmount, 0m, invoice.TotalAmount);
        invoice.AmountPaid = Math.Max(paymentTotal - refundTotal, 0m);
        invoice.RefundedAmount = refundTotal;
        invoice.LastPaymentDate = lastPayment;

        if (invoice.Status != InvoiceStatus.Cancelled)
        {
            var dueAmount = Math.Max(invoice.TotalAmount - invoice.ApprovedDiscountAmount - invoice.AmountPaid, 0m);
            invoice.Status = dueAmount <= 0m
                ? InvoiceStatus.Paid
                : invoice.AmountPaid > 0m
                    ? InvoiceStatus.Partial
                    : InvoiceStatus.Pending;
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var profile = await _db.HospitalProfiles
            .AsNoTracking()
            .OrderBy(x => x.HospitalProfileId)
            .FirstOrDefaultAsync();

        var prefix = string.IsNullOrWhiteSpace(profile?.InvoicePrefix) ? "INV" : profile!.InvoicePrefix.Trim().ToUpperInvariant();
        var startingNumber = profile?.InvoiceStartingNumber ?? 1001;
        var existingNumbers = await _db.BillingInvoices
            .AsNoTracking()
            .Select(x => x.InvoiceNumber)
            .ToListAsync();

        var maxNumber = startingNumber - 1;
        foreach (var invoiceNumber in existingNumbers)
        {
            if (!invoiceNumber.StartsWith($"{prefix}-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = invoiceNumber[(prefix.Length + 1)..];
            if (int.TryParse(suffix, out var parsed) && parsed > maxNumber)
            {
                maxNumber = parsed;
            }
        }

        var nextNumber = maxNumber + 1;
        var width = Math.Max(startingNumber.ToString().Length, 4);
        return $"{prefix}-{nextNumber.ToString($"D{width}")}";
    }

    private async Task<List<BillingInvoiceDto>> BuildInvoiceDtosAsync(bool includeDetails)
    {
        var invoices = await _db.BillingInvoices
            .AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.BillingInvoiceId)
            .ToListAsync();

        return await MapInvoicesAsync(invoices, includeDetails);
    }

    private async Task<BillingInvoiceDto?> BuildInvoiceDtoAsync(long billingInvoiceId, bool includeDetails)
    {
        var invoice = await _db.BillingInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BillingInvoiceId == billingInvoiceId);

        if (invoice is null)
        {
            return null;
        }

        return (await MapInvoicesAsync(new List<BillingInvoice> { invoice }, includeDetails)).FirstOrDefault();
    }

    private async Task<List<BillingInvoiceDto>> MapInvoicesAsync(IReadOnlyList<BillingInvoice> invoices, bool includeDetails)
    {
        if (invoices.Count == 0)
        {
            return new List<BillingInvoiceDto>();
        }

        var patientIds = invoices.Where(x => x.PatientId.HasValue).Select(x => x.PatientId!.Value).Distinct().ToList();
        var patients = await _db.Patients
            .AsNoTracking()
            .Where(x => patientIds.Contains(x.PatientId))
            .ToDictionaryAsync(x => x.PatientId);

        var userIds = patients.Values.Select(x => x.UserId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        var branchIds = invoices.Where(x => x.BranchId.HasValue).Select(x => x.BranchId!.Value).Distinct().ToList();
        var branches = await _db.Branches
            .AsNoTracking()
            .Where(x => branchIds.Contains(x.BranchId))
            .ToDictionaryAsync(x => x.BranchId);

        var partnerIds = invoices.Where(x => x.BillingPartnerId.HasValue).Select(x => x.BillingPartnerId!.Value).Distinct().ToList();
        var partners = await _db.BillingPartners
            .AsNoTracking()
            .Where(x => partnerIds.Contains(x.BillingPartnerId))
            .ToDictionaryAsync(x => x.BillingPartnerId);

        var ruleIds = invoices.Where(x => x.BillingRuleId.HasValue).Select(x => x.BillingRuleId!.Value).Distinct().ToList();
        var rules = await _db.BillingRules
            .AsNoTracking()
            .Where(x => ruleIds.Contains(x.BillingRuleId))
            .ToDictionaryAsync(x => x.BillingRuleId);

        var invoiceIds = invoices.Select(x => x.BillingInvoiceId).ToList();

        var items = includeDetails
            ? await _db.BillingInvoiceItems
                .AsNoTracking()
                .Where(x => invoiceIds.Contains(x.BillingInvoiceId))
                .OrderBy(x => x.BillingInvoiceItemId)
                .ToListAsync()
            : new List<BillingInvoiceItem>();

        var payments = includeDetails
            ? await _db.BillingInvoicePayments
                .AsNoTracking()
                .Where(x => invoiceIds.Contains(x.BillingInvoiceId))
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.BillingInvoicePaymentId)
                .ToListAsync()
            : new List<BillingInvoicePayment>();

        var refunds = includeDetails
            ? await _db.BillingRefunds
                .AsNoTracking()
                .Where(x => invoiceIds.Contains(x.BillingInvoiceId))
                .OrderByDescending(x => x.RequestedAt)
                .ThenByDescending(x => x.BillingRefundId)
                .ToListAsync()
            : new List<BillingRefund>();

        var paymentMethodIds = new HashSet<long>(
            payments.Select(x => x.BillingPaymentMethodId)
                .Concat(refunds.Where(x => x.BillingPaymentMethodId.HasValue).Select(x => x.BillingPaymentMethodId!.Value)));

        var paymentMethods = await _db.BillingPaymentMethods
            .AsNoTracking()
            .Where(x => paymentMethodIds.Contains(x.BillingPaymentMethodId))
            .ToDictionaryAsync(x => x.BillingPaymentMethodId);

        return invoices.Select(invoice =>
        {
            patients.TryGetValue(invoice.PatientId ?? 0, out var patient);
            users.TryGetValue(patient?.UserId ?? 0, out var user);
            branches.TryGetValue(invoice.BranchId ?? 0, out var branch);
            partners.TryGetValue(invoice.BillingPartnerId ?? 0, out var partner);
            rules.TryGetValue(invoice.BillingRuleId ?? 0, out var rule);

            var invoiceItems = items
                .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId)
                .Select(MapInvoiceItem)
                .ToList();

            var invoicePayments = payments
                .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId)
                .Select(payment =>
                {
                    paymentMethods.TryGetValue(payment.BillingPaymentMethodId, out var paymentMethod);
                    return MapPayment(payment, paymentMethod);
                })
                .ToList();

            var invoiceRefunds = refunds
                .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId)
                .Select(refund =>
                {
                    BillingPaymentMethod? method = null;
                    if (refund.BillingPaymentMethodId.HasValue)
                    {
                        paymentMethods.TryGetValue(refund.BillingPaymentMethodId.Value, out method);
                    }

                    return MapRefund(refund, method);
                })
                .ToList();

            return new BillingInvoiceDto
            {
                BillingInvoiceId = invoice.BillingInvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PatientId = invoice.PatientId,
                PatientName = user?.FullName ?? "Unknown patient",
                MedicalRecordNumber = patient?.MedicalRecordNumber ?? string.Empty,
                AppointmentId = invoice.AppointmentId,
                BranchId = invoice.BranchId,
                BranchName = branch?.Name ?? "Unassigned",
                PayerType = invoice.PayerType.ToString(),
                BillingPartnerId = invoice.BillingPartnerId,
                BillingPartnerName = partner?.Name,
                BillingPartnerKind = partner?.Kind.ToString(),
                BillingRuleId = invoice.BillingRuleId,
                BillingRuleName = rule?.RuleName,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                RequestedDiscountAmount = invoice.RequestedDiscountAmount,
                ApprovedDiscountAmount = invoice.ApprovedDiscountAmount,
                RefundedAmount = invoice.RefundedAmount,
                DueAmount = Math.Max(invoice.TotalAmount - invoice.ApprovedDiscountAmount - invoice.AmountPaid, 0m),
                Status = invoice.Status.ToString(),
                ClaimStatus = invoice.ClaimStatus.ToString(),
                ClaimReferenceNumber = invoice.ClaimReferenceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                LastPaymentDate = invoice.LastPaymentDate,
                ClaimSubmittedAt = invoice.ClaimSubmittedAt,
                ClaimSettledAt = invoice.ClaimSettledAt,
                DiscountNotes = invoice.DiscountNotes,
                Notes = invoice.Notes,
                ItemCount = invoiceItems.Count,
                Items = invoiceItems,
                Payments = invoicePayments,
                Refunds = invoiceRefunds
            };
        }).ToList();
    }

    private static BillingChargeDefinitionDto MapChargeDefinition(BillingChargeDefinition item)
    {
        return new BillingChargeDefinitionDto
        {
            BillingChargeDefinitionId = item.BillingChargeDefinitionId,
            ChargeType = item.ChargeType.ToString(),
            Name = item.Name,
            Code = item.Code,
            Description = item.Description,
            UnitLabel = item.UnitLabel,
            DefaultAmount = item.DefaultAmount,
            IsActive = item.IsActive
        };
    }

    private static BillingPaymentMethodDto MapPaymentMethod(BillingPaymentMethod item)
    {
        return new BillingPaymentMethodDto
        {
            BillingPaymentMethodId = item.BillingPaymentMethodId,
            Name = item.Name,
            MethodType = item.MethodType.ToString(),
            ProviderName = item.ProviderName,
            RequiresReference = item.RequiresReference,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
    }

    private static BillingPartnerDto MapPartner(BillingPartner item)
    {
        return new BillingPartnerDto
        {
            BillingPartnerId = item.BillingPartnerId,
            Kind = item.Kind.ToString(),
            Name = item.Name,
            Code = item.Code,
            ContactPerson = item.ContactPerson,
            ContactEmail = item.ContactEmail,
            ContactPhone = item.ContactPhone,
            CreditLimit = item.CreditLimit,
            ClaimSubmissionMode = item.ClaimSubmissionMode,
            Notes = item.Notes,
            IsActive = item.IsActive
        };
    }

    private static BillingRuleDto MapBillingRule(BillingRule item, BillingPartner partner)
    {
        return new BillingRuleDto
        {
            BillingRuleId = item.BillingRuleId,
            BillingPartnerId = item.BillingPartnerId,
            BillingPartnerName = partner.Name,
            BillingPartnerKind = partner.Kind.ToString(),
            RuleName = item.RuleName,
            PolicyName = item.PolicyName,
            DiscountPercentage = item.DiscountPercentage,
            CoPayPercentage = item.CoPayPercentage,
            CreditLimit = item.CreditLimit,
            ClaimSubmissionWindowDays = item.ClaimSubmissionWindowDays,
            RequiresPreApproval = item.RequiresPreApproval,
            Notes = item.Notes,
            IsActive = item.IsActive
        };
    }

    private static BillingInvoiceItemDto MapInvoiceItem(BillingInvoiceItem item)
    {
        return new BillingInvoiceItemDto
        {
            BillingInvoiceItemId = item.BillingInvoiceItemId,
            BillingChargeDefinitionId = item.BillingChargeDefinitionId,
            ChargeType = item.ChargeType.ToString(),
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.DiscountAmount,
            TotalAmount = item.TotalAmount,
            Notes = item.Notes
        };
    }

    private static BillingPaymentDto MapPayment(BillingInvoicePayment item, BillingPaymentMethod? paymentMethod)
    {
        return new BillingPaymentDto
        {
            BillingInvoicePaymentId = item.BillingInvoicePaymentId,
            BillingPaymentMethodId = item.BillingPaymentMethodId,
            PaymentMethodName = paymentMethod?.Name ?? "Unknown method",
            PaymentMethodType = paymentMethod?.MethodType.ToString() ?? PaymentMethodType.Cash.ToString(),
            Amount = item.Amount,
            PaymentDate = item.PaymentDate,
            ReferenceNumber = item.ReferenceNumber,
            Notes = item.Notes
        };
    }

    private static BillingRefundDto MapRefund(BillingRefund item, BillingPaymentMethod? paymentMethod)
    {
        return new BillingRefundDto
        {
            BillingRefundId = item.BillingRefundId,
            BillingPaymentMethodId = item.BillingPaymentMethodId,
            PaymentMethodName = paymentMethod?.Name,
            Amount = item.Amount,
            Status = item.Status.ToString(),
            Reason = item.Reason,
            Notes = item.Notes,
            RequestedAt = item.RequestedAt,
            ProcessedAt = item.ProcessedAt
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal ClampPercentage(decimal value) => Math.Clamp(value, 0m, 100m);

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }
}
