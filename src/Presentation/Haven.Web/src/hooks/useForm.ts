import { useState, useCallback } from 'react'
import { useFormErrors } from './useFormErrors'

interface UseFormOptions<T> {
  initialValues: T
  onSubmit: (values: T) => Promise<void>
  onSuccess?: () => void
}

export function useForm<T extends Record<string, any>>({
  initialValues,
  onSubmit,
  onSuccess,
}: UseFormOptions<T>) {
  const { parseApiError } = useFormErrors()
  const [values, setValues] = useState(initialValues)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [submitError, setSubmitError] = useState<string>()
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = useCallback(
    async (e?: React.FormEvent<HTMLFormElement>) => {
      e?.preventDefault()

      try {
        setIsLoading(true)
        setFieldErrors({})
        setSubmitError(undefined)

        await onSubmit(values)

        setValues(initialValues)
        onSuccess?.()
      } catch (err) {
        const { fieldErrors: newFieldErrors, submitError: newSubmitError } =
          parseApiError(err)
        setFieldErrors(newFieldErrors)
        setSubmitError(newSubmitError)
      } finally {
        setIsLoading(false)
      }
    },
    [values, initialValues, onSubmit, onSuccess, parseApiError],
  )

  const updateField = useCallback(
    (field: keyof T, value: any) => {
      setValues((prev) => ({ ...prev, [field]: value }))
      // Clear field error when user starts typing
      if (fieldErrors[field as string]) {
        setFieldErrors((prev) => {
          const next = { ...prev }
          delete next[field as string]
          return next
        })
      }
    },
    [fieldErrors],
  )

  const reset = useCallback(() => {
    setValues(initialValues)
    setFieldErrors({})
    setSubmitError(undefined)
  }, [initialValues])

  return {
    values,
    fieldErrors,
    submitError,
    isLoading,
    handleSubmit,
    updateField,
    reset,
  }
}
