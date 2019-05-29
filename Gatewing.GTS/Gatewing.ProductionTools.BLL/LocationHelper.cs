using System;
using System.Device.Location;
using System.Globalization;

namespace Gatewing.ProductionTools.BLL
{
    public class LocationHelper
    {
        #region GPS calculations
        /// <summary>
        /// his routine calculates the distance between two points (given the latitude/longitude of those points). 
        /// 
        /// Definitions:                                                    
        ///       South latitudes are negative, east longitudes are positive 
        /// </summary>
        /// <param name="coord1"></param>
        /// <param name="coord2"></param>
        /// <returns></returns>
        public static double GetDistanceBetween(GeoCoordinate coord1, GeoCoordinate coord2)
        {
            return GetDistanceBetween(coord1.Latitude, coord1.Longitude, coord2.Latitude, coord2.Longitude);
        }

        /// <summary>
        /// This routine calculates the distance between two points (given the latitude/longitude of those points). 
        /// 
        /// Definitions:                                                    
        ///       South latitudes are negative, east longitudes are positive 
        /// </summary>
        /// <param name="lat1">Latitude of point 1 (in decimal degrees)</param>
        /// <param name="lon1">Longitude of point 1 (in decimal degrees)</param>
        /// <param name="lat2">Latitude of point 2 (in decimal degrees)</param>
        /// <param name="lon2">Longitude of point 2 (in decimal degrees)</param>
        /// <returns></returns>
        public static double GetDistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(DegreesToRadians(lat1)) * Math.Sin(DegreesToRadians(lat2)) + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) * Math.Cos(DegreesToRadians(theta));

            dist = Math.Acos(dist);
            dist = RadiansToDegrees(dist);
            dist = dist * 60 * 1.1515;

            return (dist * 1.609344) * 1000;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="landingLongitude"></param>
        /// <param name="landingLatitude"></param>
        /// <param name="referenceLongitude"></param>
        /// <param name="referenceLatitude"></param>
        /// <returns></returns>
        public static int CalculateDistanceBetweenLocations(double landingLongitude, double landingLatitude, double referenceLongitude, double referenceLatitude, GeoCoordinate fixedLandingLocation)
        {
            // determine actual landing location
            var actualLandingLoc = new GeoCoordinate(landingLatitude, landingLongitude);

            // determine a reference position a little earlier in the AC's current path, this will be used to differentiate between under and overshoot
            var referenceLoc = new GeoCoordinate(referenceLatitude, referenceLongitude);

            // calculate distance from actual landing to programmed landing
            var distance = GetDistanceBetween(fixedLandingLocation.Latitude, fixedLandingLocation.Longitude, actualLandingLoc.Latitude, actualLandingLoc.Longitude);

            // calculate if its an overshoot or an undershoot:
            // if the distance between reference and programmed is smaller than the distance between actual and programmed then it's an overshoot
            var distanceBetweenReferenceAndProgrammed = GetDistanceBetween(referenceLoc, fixedLandingLocation);
            if (distanceBetweenReferenceAndProgrammed < distance)
                distance = distance * -1;

            return Convert.ToInt32(Math.Round(distance, 0));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="landingLongitude"></param>
        /// <param name="landingLatitude"></param>
        /// <param name="referenceLongitude"></param>
        /// <param name="referenceLatitude"></param>
        /// <returns></returns>
        public static int CalculateDistanceBetweenLocations(string landingLongitude, string landingLatitude, string referenceLongitude, string referenceLatitude, GeoCoordinate fixedLandingLocation)
        {
            // convert strings to double 
            var landingLongitudeD = Double.Parse(landingLongitude, System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
            var landingLatitudeD = Double.Parse(landingLatitude, System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
            var referenceLongitudeD = Double.Parse(referenceLongitude, System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
            var referenceLatitudeD = Double.Parse(referenceLatitude, System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));

            // and call overload
            return CalculateDistanceBetweenLocations(landingLongitudeD, landingLatitudeD, referenceLongitudeD, referenceLatitudeD, fixedLandingLocation);
        }

        /// <summary>
        /// This function converts decimal degrees to radians 
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        /// <summary>
        /// This function converts radians to decimal degrees 
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double RadiansToDegrees(double rad)
        {
            return (rad / Math.PI * 180.0);
        }
        #endregion
    }
}
