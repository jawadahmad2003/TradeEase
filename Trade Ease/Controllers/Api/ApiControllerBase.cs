using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TradeEase.Controllers.Api;

[Authorize]
public abstract class ApiControllerBase : ControllerBase;
