# Manual Test Cases - Scanner Profile System

This document provides structured manual test cases for the scanner profile migration.

## Test Environment Setup

**Prerequisites:**
- PostgreSQL running with test database
- API running on `http://localhost:3624`
- Admin app running
- Client app running
- Test scanner directories configured

---

## Phase 1: Database & Profile Seeding

### TC-001: Verify Migration Applied
**Objective:** Confirm database migration created profile tables

**Steps:**
1. Connect to PostgreSQL database
2. Run: `SELECT * FROM "ScannerProfiles";`
3. Run: `SELECT * FROM "ProfileConfigurations";`

**Expected Result:**
- Both tables exist
- 3 seeded profiles present (HS-1800, SP-500, SP-3000)
- HS-1800 has 2 configuration entries (CompletionDelaySeconds=1, DirectoryPattern)

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-002: Scanner Profile Relationship
**Objective:** Verify Scanner.ProfileId foreign key works

**Steps:**
1. Query existing scanner: `SELECT "Id", "ScannerName", "ProfileId" FROM "Scanners" LIMIT 1;`
2. Note if ProfileId is null or has value

**Expected Result:**
- Query succeeds
- ProfileId column exists (may be null for existing scanners)

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

## Phase 2: Admin App - Profile Management

### TC-003: View Scanner Profiles
**Objective:** Admin can view all scanner profiles

**Steps:**
1. Open Admin app
2. Navigate to Profile Management view
3. Observe profile list

**Expected Result:**
- 3 profiles displayed: "HS-1800 Auto", "SP-500 Manual", "SP-3000 Manual"
- Each shows strategy class name and description
- UI loads without errors

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-004: Create New Profile (Valid)
**Objective:** Admin can create profile with valid strategy

**Steps:**
1. In Profile Management, click "Create New Profile"
2. Enter Profile Name: "Test Auto Profile"
3. Select Strategy: "HS1800Strategy"
4. Enter Description: "Test profile for validation"
5. Click Save

**Expected Result:**
- Profile created successfully
- New profile appears in list
- No error messages

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-005: Create New Profile (Invalid Strategy)
**Objective:** System prevents invalid strategy class names

**Steps:**
1. Click "Create New Profile"
2. Enter Profile Name: "Invalid Profile"
3. Manually enter Strategy: "NonExistentStrategy"
4. Click Save

**Expected Result:**
- Error message: "Invalid strategy class name"
- Profile NOT created
- Validation prevents save

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-006: Delete Profile (Not In Use)
**Objective:** Can delete unused profiles

**Steps:**
1. Ensure "Test Auto Profile" is not assigned to any scanner
2. Click "Delete" on "Test Auto Profile"
3. Confirm deletion

**Expected Result:**
- Profile soft-deleted (IsActive = false)
- Profile removed from active list
- Success message displayed

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-007: Delete Profile (In Use)
**Objective:** Cannot delete profile assigned to scanner

**Steps:**
1. Assign "HS-1800 Auto" profile to a scanner
2. Attempt to delete "HS-1800 Auto" profile

**Expected Result:**
- Error message: "Cannot delete profile: N scanner(s) are using this profile"
- Profile NOT deleted

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-008: Assign Profile to Scanner
**Objective:** Admin can assign profile to scanner

**Steps:**
1. Navigate to Scanner Configuration
2. Select a test scanner
3. In "Profile" dropdown, select "HS-1800 Auto"
4. Save scanner configuration

**Expected Result:**
- Profile assigned successfully
- Scanner.ProfileId updated in database
- No errors

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

## Phase 3: Client App - HS-1800 Auto Processing

### TC-009: Create Order with HS-1800 Scanner
**Objective:** Set up order for auto-processing test

**Steps:**
1. Open Client app
2. Create new order
3. Select scanner with "HS-1800 Auto" profile
4. Add customer initials: "TEST"
5. Add roll with number: 101
6. Submit order

**Expected Result:**
- Order created successfully
- Order appears in Dashboard
- Roll status: "Created"

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-010: Start Auto-Processing Watcher
**Objective:** Watcher starts when roll status changes to "Scanning In Progress"

**Steps:**
1. On Dashboard, find test order/roll
2. Click "Start Scanning" button
3. Check API logs for watcher start message

**Expected Result:**
- Roll status changes to "Scanning In Progress"
- API log shows: "Started watcher for roll {RollId}"
- No errors

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-011: Auto-Process Files (Happy Path)
**Objective:** Files automatically processed after 1-second delay

**Pre-Setup:**
1. Note scanner's WatchedDir from config
2. Manually create daily folder: `WatchedDir/YYYYMMDD/`
3. Create roll folder: `WatchedDir/YYYYMMDD/0001101/`
4. Add test images: `IMG_0001.jpg`, `IMG_0002.jpg`, `IMG_0003.jpg`

**Steps:**
1. Watcher already started (from TC-010)
2. Wait 2 seconds (1 second delay + buffer)
3. Check roll status in Client
4. Check destination directory for moved files

**Expected Result:**
- After ~1 second, roll status changes to "Processed"
- Files moved to: `DestinationDir/{WeeklyFolder}/{OrderId}/101/`
- Files renamed: `TEST-{OrderId}-101-001.jpg`, `TEST-{OrderId}-101-002.jpg`, etc.
- Source directory deleted
- Watcher stopped automatically

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-012: Auto-Process Error Handling
**Objective:** Errors logged and manual fallback available

**Pre-Setup:**
1. Start new roll scanning
2. Create daily/roll folders
3. Add file with invalid name/type

**Steps:**
1. Watcher detects directory
2. Wait for auto-processing
3. Check API logs for errors

**Expected Result:**
- Error logged in API
- Roll status remains "Scanning In Progress" or "Processing"
- "Complete Roll" button still available for manual retry

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-013: Pause Auto-Processing
**Objective:** Pausing roll stops watcher

**Steps:**
1. Start roll scanning (watcher active)
2. Click "Pause" button
3. Check API logs

**Expected Result:**
- Roll status changes to "Scanning Paused"
- API log shows: "Stopped watcher session {SessionId}"
- Watcher no longer active

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

## Phase 4: Client App - SP-500 Manual Processing

### TC-014: Create Order with SP-500 Scanner
**Objective:** Manual workflow for SP-500

**Steps:**
1. Create order with SP-500 scanner
2. Add roll with number: 201

**Expected Result:**
- Order created successfully

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-015: Start Scanning (No Watcher)
**Objective:** SP-500 profile does NOT start watcher

**Steps:**
1. Click "Start Scanning" for SP-500 roll
2. Check API logs

**Expected Result:**
- Roll status: "Scanning In Progress"
- **NO** watcher start message in logs
- Client shows "Complete Roll" button enabled

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-016: Manual Complete Roll
**Objective:** Scan tech manually triggers processing

**Pre-Setup:**
1. Create roll folder in WatchedDir: `WatchedDir/0001201/`
2. Add test images

**Steps:**
1. Wait for files to finish exporting (manual observation)
2. Click "Complete Roll" button

**Expected Result:**
- Files processed immediately
- Roll status: "Processed"
- Files moved and renamed correctly
- Source directory deleted

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

## Phase 5: API Endpoint Testing

### TC-017: GET /api/ScannerProfile/profiles
**Objective:** Returns all active profiles

**Steps:**
1. Use Postman/curl: `GET http://localhost:3624/api/ScannerProfile/profiles`
2. Include auth token

**Expected Result:**
- Status 200 OK
- JSON array with 3+ profiles
- Each profile has: Id, ProfileName, StrategyClassName, Description

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-018: GET /api/ScannerProfile/strategies
**Objective:** Returns hardcoded strategy list

**Steps:**
1. GET `http://localhost:3624/api/ScannerProfile/strategies`

**Expected Result:**
- Status 200 OK
- JSON array: ["HS1800Strategy", "SP500Strategy", "SP3000Strategy"]

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-019: POST /api/ScannerProfile/add (Valid)
**Objective:** Create profile via API

**Steps:**
1. POST to `/api/ScannerProfile/add`
2. Body:
```json
{
  "profileName": "API Test Profile",
  "strategyClassName": "SP500Strategy",
  "description": "Created via API"
}
```

**Expected Result:**
- Status 200 OK
- Profile created in database

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-020: POST /api/ScannerProfile/add (Invalid Strategy)
**Objective:** Validation prevents invalid strategy

**Steps:**
1. POST with `"strategyClassName": "FakeStrategy"`

**Expected Result:**
- Status 400 Bad Request
- Error message contains "Invalid strategy"

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

## Phase 6: Edge Cases & Resilience

### TC-021: API Restart Clears Watchers
**Objective:** In-memory watchers reset on restart

**Steps:**
1. Start roll scanning (watcher active)
2. Restart API
3. Check Client app
4. Attempt to complete roll manually

**Expected Result:**
- Watcher no longer active after restart
- Roll status still "Scanning In Progress"
- Manual "Complete Roll" works

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-022: Concurrent Rolls (Same Scanner)
**Objective:** One roll at a time per scanner

**Steps:**
1. Start scanning Roll 1 (watcher active)
2. Attempt to start Roll 2 on same scanner

**Expected Result:**
- Error message: "Other roll(s) already in progress"
- Roll 2 remains "Created"
- Only one watcher active

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-023: Daily Folder Missing (HS-1800)
**Objective:** Strategy creates daily folder if missing

**Steps:**
1. Delete today's daily folder from WatchedDir
2. Start HS-1800 roll scanning

**Expected Result:**
- Daily folder created automatically
- Watcher starts successfully
- No errors

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

### TC-024: Multiple Scanners Concurrently
**Objective:** Multiplexed watcher handles multiple scanners

**Steps:**
1. Start Roll A on Scanner 1 (HS-1800)
2. Start Roll B on Scanner 2 (SP-500)
3. Complete both rolls

**Expected Result:**
- Scanner 1 auto-processes (watcher)
- Scanner 2 requires manual complete
- Both process correctly without interference

**Status:** [ ] Pass [ ] Fail

**Notes:**
___________________________________

---

## Test Summary

**Date:** ________________
**Tester:** ________________
**Total Tests:** 24
**Passed:** _____
**Failed:** _____
**Blocked:** _____

**Critical Issues:**
_______________________________________________________________________
_______________________________________________________________________

**Notes:**
_______________________________________________________________________
_______________________________________________________________________
