using Ovation.Models;

namespace Ovation.Api
{
    internal class ApiResult<T>
    {
        internal bool Success { get; private set; }
        internal T Data { get; private set; }
        internal OvationError Error { get; private set; }

        internal static ApiResult<T> Ok(T data) => new ApiResult<T> { Success = true, Data = data };
        internal static ApiResult<T> Failure(OvationError error) => new ApiResult<T> { Success = false, Error = error };
    }
}
