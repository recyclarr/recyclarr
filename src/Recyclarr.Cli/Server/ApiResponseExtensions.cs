using Refit;

namespace Recyclarr.Cli.Server;

internal static class ApiResponseExtensions
{
    extension<T>(IApiResponse<T> response)
    {
        /// <summary>
        /// Throws the response's ApiException for any unsuccessful status, so the exception
        /// handler reports it; otherwise returns the body.
        /// </summary>
        public T ContentOrThrow()
        {
            if (response.Error is not null)
            {
                throw response.Error;
            }

            // non-null: a successful response from these endpoints always has a body
            return response.Content!;
        }
    }
}
