/**
 * Reverse geocoding: Convert GPS coordinates to a human-readable address (city name)
 * Uses Google Maps Geocoding API
 */
export async function reverseGeocode(lat: number, lng: number): Promise<string | null> {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY

  if (!apiKey) {
    console.error("[reverseGeocode] Google Maps API key missing. Add NEXT_PUBLIC_GOOGLE_MAPS_API_KEY to .env.local")
    return null
  }

  console.log(`[reverseGeocode] Reverse geocoding coordinates: ${lat}, ${lng}`)

  try {
    const url = `https://maps.googleapis.com/maps/api/geocode/json?latlng=${lat},${lng}&key=${apiKey}&language=fr`
    const response = await fetch(url)

    if (!response.ok) {
      console.error("[reverseGeocode] HTTP error:", response.status, response.statusText)
      return null
    }

    const data = await response.json()
    console.log("[reverseGeocode] API response status:", data.status)

    if (data.status !== "OK") {
      console.error("[reverseGeocode] API returned non-OK status:", data.status, data.error_message || "")
      return null
    }

    if (!data.results || data.results.length === 0) {
      console.error("[reverseGeocode] No results from geocoding API")
      return null
    }

    // Extract city name from address components
    // Priority: locality (city) > political (arrondissement) > formatted_address
    const result = data.results[0]
    const addressComponents = result.address_components || []

    // Try to find locality (city)
    const locality = addressComponents.find((component: any) =>
      component.types.includes("locality")
    )
    if (locality) {
      console.log("[reverseGeocode] Found locality:", locality.long_name)
      return locality.long_name
    }

    // Fallback: Try to find administrative_area_level_2 (département)
    const adminArea = addressComponents.find((component: any) =>
      component.types.includes("administrative_area_level_2")
    )
    if (adminArea) {
      console.log("[reverseGeocode] Found admin area:", adminArea.long_name)
      return adminArea.long_name
    }

    // Last fallback: Use formatted address (but shorten it)
    // Example: "12 Rue Example, 75001 Paris, France" -> "Paris"
    const formattedAddress = result.formatted_address || ""
    const parts = formattedAddress.split(",")

    // Try to extract city from formatted address (usually in 2nd or 3rd part)
    if (parts.length >= 2) {
      // Remove postal code if present
      const cityPart = parts[parts.length - 2].trim().replace(/^\d{5}\s*/, "")
      const extractedCity = cityPart || formattedAddress.split(",")[0].trim()
      console.log("[reverseGeocode] Extracted city from formatted address:", extractedCity)
      return extractedCity
    }

    const fallbackCity = formattedAddress.split(",")[0].trim()
    console.log("[reverseGeocode] Using first part of formatted address:", fallbackCity)
    return fallbackCity
  } catch (error) {
    console.error("[reverseGeocode] Error during reverse geocoding:", error)
    return null
  }
}
