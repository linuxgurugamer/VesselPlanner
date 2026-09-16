using System.Globalization;

namespace DeltaVEditor
{

    public class DeltaVRowEditor
    {
        public string Origin_str;
        public string Destination_str;
        public string dV_to_low_orbit_str;
        public string injection_dV_str;
        public string capture_dV_str;
        public string transfer_to_low_orbit_dV_str;
        public string total_capture_dV_str;
        public string dV_low_orbit_to_surface_str;
        public string ascent_dV_str;
        public string plane_change_dV_str;
        public string sortOrder_str;

        public bool isMoon;
        public string parent;

        public DeltaVRowEditor(DeltaVRowEditor dvre)
        {
            this.Origin_str = dvre.Origin_str;
            this.Destination_str = dvre.Destination_str;
            this.dV_to_low_orbit_str = dvre.dV_to_low_orbit_str;
            this.injection_dV_str = dvre.injection_dV_str;
            this.capture_dV_str = dvre.capture_dV_str;
            this.transfer_to_low_orbit_dV_str = dvre.dV_to_low_orbit_str;
            this.total_capture_dV_str = dvre.total_capture_dV_str;
            this.dV_low_orbit_to_surface_str = dvre.dV_low_orbit_to_surface_str;
            this.ascent_dV_str = dvre.ascent_dV_str;
            this.plane_change_dV_str = dvre.plane_change_dV_str;
            this.isMoon = dvre.isMoon;
            this.parent = dvre.parent;
        }
        public DeltaVRowEditor(
                 string Origin,
                 string Destination,
                 string dV_to_low_orbit = "-1",
                 string injection_dV = "-1",
                 string capture_dV = "-1",
                 string transfer_to_low_orbit_dV = "-1",
                 string total_capture_dV = "-1",
                 string dV_low_orbit_to_surface = "-1",
                 string ascent_dV = "-1",
                 string plane_change_dV = "-1",
                 string sortOrder = "0",
                 bool isMoon = false,
                 string parent = ""
            )
        {
            this.Origin_str = Origin;
            this.Destination_str = Destination;
            this.dV_to_low_orbit_str = dV_to_low_orbit;
            this.injection_dV_str = injection_dV;
            this.capture_dV_str = capture_dV;
            this.transfer_to_low_orbit_dV_str = transfer_to_low_orbit_dV;
            this.total_capture_dV_str = total_capture_dV;
            this.dV_low_orbit_to_surface_str = dV_low_orbit_to_surface;
            this.ascent_dV_str = ascent_dV;
            this.plane_change_dV_str = plane_change_dV;
            this.sortOrder_str = sortOrder;
            this.isMoon = isMoon;
            this.parent = parent;
        }

        public static DeltaVRowEditor FromDeltaV(VesselPlanner.Core.DeltaV dv)
        {
            return new DeltaVRowEditor
            (
                 dv.Origin,
                 dv.Destination,
                 dv.dV_to_low_orbit.ToString(CultureInfo.InvariantCulture),
                 dv.injection_dV.ToString(CultureInfo.InvariantCulture),
                 dv.capture_dV.ToString(CultureInfo.InvariantCulture),
                 dv.transfer_to_low_orbit_dV.ToString(CultureInfo.InvariantCulture),
                 dv.total_capture_dV.ToString(CultureInfo.InvariantCulture),
                 dv.dV_low_orbit_to_surface.ToString(CultureInfo.InvariantCulture),
                 dv.ascent_dV.ToString(CultureInfo.InvariantCulture),
                 dv.plane_change_dV.ToString(CultureInfo.InvariantCulture),
                 dv.sortOrder.ToString(CultureInfo.InvariantCulture),
                 dv.isMoon,
                 dv.parent
            );
        }

        public bool TryToDeltaV(out VesselPlanner.Core.DeltaV dv)
        {
            dv = null;

            if (!float.TryParse(dV_to_low_orbit_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var toOrbit)) return false;
            if (!float.TryParse(injection_dV_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var escape)) return false;

            if (!float.TryParse(capture_dV_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var captureDv)) return false;

            if (!float.TryParse(transfer_to_low_orbit_dV_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var transfer_to_low_orbit)) return false;
            if (!float.TryParse(total_capture_dV_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var total_capture)) return false;

            if (!float.TryParse(dV_low_orbit_to_surface_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var toSurface)) return false;
            if (!float.TryParse(ascent_dV_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var ascent)) return false;

            if (!float.TryParse(plane_change_dV_str, NumberStyles.Float, CultureInfo.InvariantCulture, out var planeChange)) return false;

            dv = new VesselPlanner.Core.DeltaV
            {
                Origin = Origin_str?.Trim() ?? "",
                Destination = Destination_str?.Trim() ?? "",
                dV_to_low_orbit = toOrbit,
                injection_dV = escape,
                capture_dV = captureDv,
                transfer_to_low_orbit_dV = transfer_to_low_orbit,
                total_capture_dV = total_capture,
                dV_low_orbit_to_surface = toSurface,
                ascent_dV = ascent,
                plane_change_dV = planeChange,
                sortOrder = sortOrder_str,
                parent = this.parent,
                isMoon = this.isMoon
            };
            return true;
        }
    }
}