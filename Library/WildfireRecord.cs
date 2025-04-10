namespace Library
{
    public class WildfireRecord
    {
        public string Id { get; set; }
        public string ObjectId { get; set; }
        public string Damage { get; set; }
        public string StreetNumber { get; set; }
        public string StreetName { get; set; }
        public string StreetType { get; set; }
        public string StreetSuffix { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string CalFireUnit { get; set; }
        public string County { get; set; }
        public string Community { get; set; }
        public string Battalion { get; set; }
        public string IncidentName { get; set; }
        public string IncidentNumber { get; set; }
        public string IncidentStartDate { get; set; }
        public string HazardType { get; set; }
        public string FireStartLocation { get; set; }
        public string FireCause { get; set; }
        public string DefenseActions { get; set; }
        public string StructureType { get; set; }
        public string StructureCategory { get; set; }
        public string UnitsInStructure { get; set; }
        public string DamagedOutbuildings { get; set; }
        public string NonDamagedOutbuildings { get; set; }
        public string RoofConstruction { get; set; }
        public string Eaves { get; set; }
        public string VentScreen { get; set; }
        public string ExteriorSiding { get; set; }
        public string WindowPane { get; set; }
        public string DeckPorchOnGrade { get; set; }
        public string DeckPorchElevated { get; set; }
        public string PatioCoverCarport { get; set; }
        public string FenceAttached { get; set; }
        public string PropaneTankDistance { get; set; }
        public string UtilityStructureDistance { get; set; }
        public string FireNameSecondary { get; set; }
        public string ApnParcel { get; set; }
        public string AssessedImprovedValue { get; set; }
        public string YearBuilt { get; set; }
        public string SiteAddress { get; set; }
        public string GlobalId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public bool IsLatitudeMissing { get; set; }
        public bool IsLongitudeMissing { get; set; }
    }
}