interface ValidationErrorResponse {
  success: false
  message: string
  errors: Record<string, string[]>
}

interface ApiErrorWithResponse extends Error {
  response?: {
    status: number
    data: unknown
  }
}

export function useFormErrors() {
  const parseApiError = (error: unknown): {
    fieldErrors: Record<string, string>
    submitError?: string
  } => {
    const fieldErrors: Record<string, string> = {}
    let submitError: string | undefined

    // Check if error has a response object (from fetch/axios)
    if (error && typeof error === 'object') {
      const apiError = error as ApiErrorWithResponse

      // Handle response from API
      if (apiError.response?.data) {
        const data = apiError.response.data
        if (
          data &&
          typeof data === 'object' &&
          'errors' in data &&
          typeof data.errors === 'object'
        ) {
          const validationResponse = data as ValidationErrorResponse
          // Flatten field errors (take first error per field)
          for (const [field, messages] of Object.entries(
            validationResponse.errors,
          )) {
            if (Array.isArray(messages) && messages.length > 0) {
              fieldErrors[field] = messages[0]
            }
          }
          submitError = validationResponse.message
          return { fieldErrors, submitError }
        }
      }

      // Check if it's already a validation error response object
      if ('errors' in apiError && typeof apiError.errors === 'object') {
        const validationResponse = apiError as ValidationErrorResponse
        for (const [field, messages] of Object.entries(validationResponse.errors)) {
          if (Array.isArray(messages) && messages.length > 0) {
            fieldErrors[field] = messages[0]
          }
        }
        submitError = validationResponse.message
        return { fieldErrors, submitError }
      }

      // Fallback to error message
      if (apiError instanceof Error) {
        submitError = apiError.message
      }
    }

    return { fieldErrors, submitError }
  }

  return { parseApiError }
}
