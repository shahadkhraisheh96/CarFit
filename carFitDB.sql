CREATE TABLE Users (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    email NVARCHAR(255) UNIQUE NOT NULL,
    password NVARCHAR(255) NOT NULL,
    created_at DATETIME DEFAULT GETDATE()
);

CREATE TABLE UserProfiles (
    user_id INT PRIMARY KEY,
    age INT,
    marital_status NVARCHAR(50),
    has_kids BIT,
    kids_count INT DEFAULT 0,
    purpose NVARCHAR(100), -- e.g., Commuting, Family, Off-road
    trip_type NVARCHAR(100),
    budget_min DECIMAL(18, 2),
    budget_max DECIMAL(18, 2),
    payment_method NVARCHAR(50), -- Cash/Installment
    installment_months INT,
    preference_new_used NVARCHAR(20),
    transmission_pref NVARCHAR(20),
    size_pref NVARCHAR(50),
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE
);
CREATE TABLE Sellers (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(20),
    type NVARCHAR(20), -- Dealer / Individual
    location NVARCHAR(255)
);

CREATE TABLE Cars (
    id INT PRIMARY KEY IDENTITY(1,1),
    make NVARCHAR(50) NOT NULL,
    model NVARCHAR(50) NOT NULL,
    year INT NOT NULL,
    price DECIMAL(18, 2),
    type NVARCHAR(20), -- New / Used
    transmission NVARCHAR(20),
    fuel_type NVARCHAR(20),
    size_category NVARCHAR(50),
    seats INT,
    fuel_efficiency NVARCHAR(50),
    images NVARCHAR(MAX) -- Stores JSON array or comma-separated URLs
);

CREATE TABLE CarListings (
    id INT PRIMARY KEY IDENTITY(1,1),
    car_id INT,
    seller_id INT,
    listing_price DECIMAL(18, 2),
    available BIT DEFAULT 1,
    installment_option BIT DEFAULT 0,
    FOREIGN KEY (car_id) REFERENCES Cars(id),
    FOREIGN KEY (seller_id) REFERENCES Sellers(id)
);
CREATE TABLE InspectionReports (
    car_id INT PRIMARY KEY,
    center_name NVARCHAR(100),
    inspection_date DATE,
    chassis_1_status NVARCHAR(50),
    chassis_2_status NVARCHAR(50),
    chassis_3_status NVARCHAR(50),
    chassis_4_status NVARCHAR(50),
    body_condition NVARCHAR(MAX),
    roof_condition NVARCHAR(100),
    engine_health_percent INT,
    engine_smoke BIT,
    gearbox_status NVARCHAR(100),
    paint_filler_status NVARCHAR(MAX),
    carseer_attached BIT DEFAULT 0,
    overall_score DECIMAL(3, 2), -- e.g., 4.5
    FOREIGN KEY (car_id) REFERENCES Cars(id) ON DELETE CASCADE
);

CREATE TABLE InspectionTermsGlossary (
    term NVARCHAR(100) PRIMARY KEY,
    severity_level NVARCHAR(20), -- Low, Medium, High, Critical
    explanation_ar NVARCHAR(MAX),
    explanation_en NVARCHAR(MAX)
);
CREATE TABLE SavedResults (
    user_id INT,
    car_id INT,
    saved_at DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (user_id, car_id),
    FOREIGN KEY (user_id) REFERENCES Users(id),
    FOREIGN KEY (car_id) REFERENCES Cars(id)
);

CREATE TABLE RecommendationLog (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    recommended_car_ids NVARCHAR(MAX), -- Stored as a string list or JSON
    score DECIMAL(5, 2),
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);
ALTER TABLE Cars ADD kilometers NVARCHAR(100) NULL;

-- A. ADD BRAND NEW COLUMNS (These did not exist in your original schema)
ALTER TABLE Cars ADD scraped_id INT NULL;
ALTER TABLE Cars ADD trim NVARCHAR(100) NULL;
ALTER TABLE Cars ADD kilometers NVARCHAR(100) NULL; -- <-- Fixed! Added fresh instead of modified
ALTER TABLE Cars ADD engine_size NVARCHAR(100) NULL;
ALTER TABLE Cars ADD exterior_color NVARCHAR(100) NULL;
ALTER TABLE Cars ADD interior_color NVARCHAR(100) NULL;
ALTER TABLE Cars ADD regional_specs NVARCHAR(100) NULL;
ALTER TABLE Cars ADD interior_options NVARCHAR(MAX) NULL;
ALTER TABLE Cars ADD exterior_options NVARCHAR(MAX) NULL;
ALTER TABLE Cars ADD technology_options NVARCHAR(MAX) NULL;

-- B. ALTER / MODIFY EXISTING COLUMNS (These existed in your original schema)
-- Rename your original 'size_category' column to 'body_type' to match the dataset
EXEC sp_rename 'Cars.size_category', 'body_type', 'COLUMN';
ALTER TABLE Cars ALTER COLUMN body_type NVARCHAR(100) NULL;

-- Safely add brand new columns only if they do not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'scraped_id')
    ALTER TABLE Cars ADD scraped_id INT NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'trim')
    ALTER TABLE Cars ADD trim NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'kilometers')
    ALTER TABLE Cars ADD kilometers NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'engine_size')
    ALTER TABLE Cars ADD engine_size NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'exterior_color')
    ALTER TABLE Cars ADD exterior_color NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'interior_color')
    ALTER TABLE Cars ADD interior_color NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'regional_specs')
    ALTER TABLE Cars ADD regional_specs NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'interior_options')
    ALTER TABLE Cars ADD interior_options NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'exterior_options')
    ALTER TABLE Cars ADD exterior_options NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'technology_options')
    ALTER TABLE Cars ADD technology_options NVARCHAR(MAX) NULL;

-- Safely rename 'size_category' to 'body_type' only if 'size_category' still exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cars' AND COLUMN_NAME = 'size_category')
BEGIN
    EXEC sp_rename 'Cars.size_category', 'body_type', 'COLUMN';
    ALTER TABLE Cars ALTER COLUMN body_type NVARCHAR(100) NULL;
END

-- Drop the location column only if it hasn't been dropped yet
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sellers' AND COLUMN_NAME = 'location')
    ALTER TABLE Sellers DROP COLUMN location; 

-- Add city and neighborhood breakdowns safely
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sellers' AND COLUMN_NAME = 'city')
    ALTER TABLE Sellers ADD city NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sellers' AND COLUMN_NAME = 'neighborhood')
    ALTER TABLE Sellers ADD neighborhood NVARCHAR(100) NULL
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CarListings' AND COLUMN_NAME = 'payment_method_allowed')
    ALTER TABLE CarListings ADD payment_method_allowed NVARCHAR(100) NULL;

    -- Safely alter body_condition data type
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InspectionReports' AND COLUMN_NAME = 'body_condition')
    ALTER TABLE InspectionReports ALTER COLUMN body_condition NVARCHAR(255) NULL;

-- Add new inspection metrics safely
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InspectionReports' AND COLUMN_NAME = 'paint_status')
    ALTER TABLE InspectionReports ADD paint_status NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InspectionReports' AND COLUMN_NAME = 'description_score')
    ALTER TABLE InspectionReports ADD description_score NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InspectionReports' AND COLUMN_NAME = 'calculated_trust_score')
    ALTER TABLE InspectionReports ADD calculated_trust_score DECIMAL(3, 2) NULL;

    -- Speed up queries filtering available cars by price and type
CREATE NONCLUSTERED INDEX IX_Cars_Matching 
ON Cars (transmission, body_type, year) 
INCLUDE (make, model, price);

-- Speed up listings lookups to ensure we only pull available cars
CREATE NONCLUSTERED INDEX IX_CarListings_Availability 
ON CarListings (available) 
INCLUDE (car_id, seller_id, listing_price);

-- Speed up user authentication and profile lookups
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users (email);
-- 1. Double check that paint_status actually exists as a column. If not, add it.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InspectionReports' AND COLUMN_NAME = 'paint_status')
BEGIN
    ALTER TABLE InspectionReports ADD paint_status NVARCHAR(255) NULL;
END
GO

-- 2. Drop the old view if it was partially created
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AvailableCarDetails')
    DROP VIEW vw_AvailableCarDetails;
GO

-- 3. Create the corrected View mapping 'paint_status' properly
CREATE VIEW vw_AvailableCarDetails AS
SELECT 
    C.id AS CarId,
    C.scraped_id,
    C.make,
    C.model,
    C.trim,
    C.year,
    C.kilometers,
    C.body_type,
    C.seats,
    C.fuel_type,
    C.transmission,
    C.engine_size,
    C.exterior_color,
    C.interior_color,
    C.regional_specs,
    C.price,
    C.interior_options,
    C.exterior_options,
    C.technology_options,
    C.images,
    CL.id AS ListingId,
    CL.listing_price,
    CL.payment_method_allowed,
    S.id AS SellerId,
    S.name AS SellerName,
    S.city,
    S.neighborhood,
    IR.body_condition,
    IR.paint_status,        -- Aligned directly with the InspectionReports table
    IR.description_score,
    ISNULL(IR.calculated_trust_score, 3.0) AS TrustScore
FROM Cars C
INNER JOIN CarListings CL ON C.id = CL.car_id
INNER JOIN Sellers S ON CL.seller_id = S.id
LEFT JOIN InspectionReports IR ON C.id = IR.car_id
WHERE CL.available = 1;
GO

CREATE PROCEDURE GetCarMatchesForUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Get the user profile criteria into variables
    DECLARE @BudgetMax DECIMAL(18,2), @TransPref NVARCHAR(100), @SizePref NVARCHAR(100), @HasKids BIT;
    
    SELECT 
        @BudgetMax = budget_max,
        @TransPref = transmission_pref,
        @SizePref = size_pref,
        @HasKids = has_kids
    FROM UserProfiles 
    WHERE user_id = @UserId;

    -- 2. Execute the weighted recommendation match
    SELECT 
        CarId,
        make,
        model,
        year,
        listing_price,
        city,
        body_condition,
        description_score,
        TrustScore,
        -- Calculate the Match Percentage dynamically
        (
            -- Condition 1: Size Category Match (30 Points max)
            CASE WHEN body_type = @SizePref THEN 30 ELSE 10 END +
            
            -- Condition 2: Structural Integrity / Trust Score (50 Points max: TrustScore is out of 5.0, so multiply by 10)
            (TrustScore * 10) +
            
            -- Condition 3: Family Seat Check (20 Points bonus if user has kids and car has $\ge$ 5 seats)
            CASE WHEN @HasKids = 1 AND seats >= 5 THEN 20 ELSE 0 END
        ) AS DynamicMatchScore
    FROM vw_AvailableCarDetails
    WHERE listing_price <= @BudgetMax  -- Hard Filter: Must be within budget
      AND transmission = @TransPref    -- Hard Filter: Must match transmission comfort
    ORDER BY DynamicMatchScore DESC;   -- Highest matches first!
END;


-- 1. Clear out the legacy 1:1 user profile table
DROP TABLE IF EXISTS UserProfiles;
GO

-- 2. Create the Multi-Profile enabled table supporting Identity GUID keys
CREATE TABLE UserProfiles (
    profile_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id NVARCHAR(450) NOT NULL, -- Ties explicitly to your ASP.NET Core Identity string Id 
    profile_name NVARCHAR(100) NOT NULL DEFAULT 'My Fit Profile', -- e.g., "Daily Commute", "Family Weekend"
    age INT NULL,
    marital_status NVARCHAR(50) NULL,
    has_kids BIT DEFAULT 0,
    kids_count INT DEFAULT 0,
    purpose NVARCHAR(100) NULL, 
    budget_min DECIMAL(18, 2) DEFAULT 0,
    budget_max DECIMAL(18, 2) NOT NULL,
    payment_method NVARCHAR(50) NULL, 
    transmission_pref NVARCHAR(20) NOT NULL,
    size_pref NVARCHAR(50) NOT NULL,
    is_active BIT DEFAULT 1
);
GO

-- 3. Update the Recommendation Engine Stored Procedure to calculate matches by Profile ID [cite: 85, 86]
ALTER PROCEDURE GetCarMatchesForUser
    @ProfileId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BudgetMax DECIMAL(18,2), @TransPref NVARCHAR(100), @SizePref NVARCHAR(100), @HasKids BIT;
    
    -- Pull constraints based on the specific profile persona [cite: 86]
    SELECT 
        @BudgetMax = budget_max,
        @TransPref = transmission_pref,
        @SizePref = size_pref,
        @HasKids = has_kids
    FROM UserProfiles 
    WHERE profile_id = @ProfileId;

    -- Calculate the dynamic recommendation score using your custom weighting [cite: 86]
    SELECT 
        CarId,
        make,
        model,
        year,
        listing_price,
        city,
        body_condition,
        description_score,
        TrustScore,
        (
            -- Condition 1: Body Archetype Match (30 Points Max) [cite: 86]
            CASE WHEN body_type = @SizePref THEN 30 ELSE 10 END +
            
            -- Condition 2: Structural Mechanics Trust Score (50 Points Max) [cite: 86]
            (TrustScore * 10) +
            
            -- Condition 3: Family Space Assessment (20 Points Max) [cite: 86]
            CASE WHEN @HasKids = 1 AND seats >= 5 THEN 20 ELSE 0 END
        ) AS DynamicMatchScore
    FROM vw_AvailableCarDetails
    WHERE listing_price <= @BudgetMax  -- Hard Budget Constraint [cite: 86]
      AND transmission = @TransPref    -- Hard Mechanical Constraint [cite: 86]
    ORDER BY DynamicMatchScore DESC;   
END;
GO