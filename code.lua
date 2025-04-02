---@diagnostic disable: lowercase-global, undefined-global
-- ==================================
-- F1 MANAGER 2024 DATA EXPORTER
-- ==================================


-- ==================================
-- CONFIGURATION
-- ==================================
--#region

local fullPointerPath = "[[[[[[F1Manager24.exe+798F570]+150]+3E8]+130]+0]+28]+0"
local SHARED_MEM_NAME = "F1Manager_Telemetry"
local SHARED_MEM_SIZE = 65536  -- 64KB buffer (adjust as needed)
-- CAR STRUCTURE OFFSETS

-- ENUM MAPPINGS
local carscarOffsets = {
    -- Ferrari
    Ferrari1 = 0x0000,
    Ferrari2 = 0x10D8,
    
    -- McLaren
    McLaren1 = 0x21B0,
    McLaren2 = 0x3288,
    
    -- Red Bull
    RedBull1 = 0x4360,
    RedBull2 = 0x5438,
    
    -- Mercedes
    Mercedes1 = 0x6510,
    Mercedes2 = 0x75E8,
    
    -- Alpine
    Alpine1 = 0x86C0,
    Alpine2 = 0x9798,
    
    -- Williams
    Williams1 = 0xA870,
    Williams2 = 0xB948,
    
    -- Haas
    Haas1 = 0xCA20,
    Haas2 = 0xDAF8,
    
    -- Racing Bulls
    RacingBulls1 = 0xEBD0,
    RacingBulls2 = 0xFCA8,
    
    -- Kick Sauber
    KickSauber1 = 0x10D80,
    KickSauber2 = 0x11E58,
    
    -- Aston Martin
    AstonMartin1 = 0x12F30,
    AstonMartin2 = 0x14008,
    
    -- MyTeam
    MyTeam1 = 0x150E0,
    MyTeam2 = 0x161B8
}

local tyreCompoundMap = {
    [0]="Soft",[1]="Soft",[2]="Soft",[3]="Soft",[4]="Soft",[5]="Soft",[6]="Soft",[7]="Soft",
    [8]="Medium",[9]="Medium",[10]="Medium",
    [11]="Hard",[12]="Hard",
    [13]="Inter",[14]="Inter",[15]="Inter",[16]="Inter",[17]="Inter",
    [18]="Wet",[19]="Wet"
}

local paceModeMap = {
    [4]="Conserve",[3]="Light",[2]="Standard",[1]="Aggressive",[0]="Attack"
}

local fuelModeMap = {
    [2]="Conserve",[1]="Balanced",[0]="Push"
}

local ersModeMap = {
    [0]="Neutral",[1]="Harvest",[2]="Deploy",[3]="Top Up"
}

-- CAR STRUCTURE OFFSETS
local carOffsets = {
    pilotDataptr = 0x708, -- 8-byte
    currentLap = 0x7E4, --DONE
    tyreCompound = 0xED5, --DONE
    paceMode = 0xEF1, --DONE
    fuelMode = 0xEF0, --DONE
    ersMode = 0xEF2, --DONE
    flTemp = 0x980, flDeg = 0x984, --DONE
    frTemp = 0x98C, frDeg = 0x990, --DONE
    rlTemp = 0x998, rlDeg = 0x99C, --DONE
    rrTemp = 0x9A4, rrDeg = 0x9A8, --DONE
    engTemp = 0x77C, engDeg = 0x784, --DONE
    gearboxDeg = 0x78C, --DONE
    ersDeg = 0x788, --DONE
    charge = 0x878, --DONE
    fuel = 0x778 --DONE
}

local driverOffsets = {
    sessionDataptr = 0x940,
    driverNumber = 0x58C, --DONE
    turnNumber = 0x530, --DONE
    speed = 0x4F0, --DONE
    rpm = 0x4EC, --DONE
    gear = 0x524, -- DONE
    driverBestLap = 0x538, --DONE
    lastLapTime = 0x540, --DONE
    currentLapTime = 0x544, --DONE
    lastS1Time = 0x548, --DONE
    lastS2Time = 0x550, --DONE
    lastS3Time = 0x558 --DONE
}

local sessionOffsets = {
    weatherDataptr = 0xA12990,
    timeElasped = 0x148, --DONE
    rubber = 0x278, --DONE
    bestSessionTime = 0x768 --DONE
}

local weatherOffsets = {
    airTemp = 0xAC, --DONE
    trackTemp = 0xB0 --DONE
}

local mmf = io.open(SHARED_MEM_NAME, "w+b")
if not mmf then
    print("Failed to create shared memory file")
    return
end

mmf:write(string.rep("\0", SHARED_MEM_SIZE))
mmf:flush()
--#endregion

-- ==================================
-- DATA COLLECTION
-- ==================================
--#region

function readMemorySafe(address, offset, readFunc)
    if not address or address == 0 then return nil end
    local success, value = pcall(readFunc, address + (offset or 0))
    return success and value or nil
end

function readPointer8(address, offset)
    if not address or address == 0 then return nil end
    local value = readQword(address + (offset or 0))
    return value ~= 0 and value or nil
end

function collectDriverData(car)
    local carsBase = getAddress(fullPointerPath)
    if not carsBase or carsBase == 0 then sleep(500) return nil end
    
    local carBase = carsBase + carscarOffsets[car]
    if not carBase or carBase == 0 then return nil end

    local driverBase = readPointer8(carBase, carOffsets.pilotDataptr)
    if not driverBase or driverBase == 0 then return nil end

    local sessionBase = readPointer8(driverBase, driverOffsets.sessionDataptr)
    if not sessionBase or sessionBase == 0 then return nil end

    local weatherBase = readPointer8(sessionBase, sessionOffsets.weatherDataptr)
    if not weatherBase or weatherBase == 0 then return nil end

    return {
        timeElasped = readMemorySafe(sessionBase, sessionOffsets.timeElasped, readFloat) or 0,
        driverNumber = readMemorySafe(driverBase, driverOffsets.driverNumber, readByte) or 0,
        currentLap = readMemorySafe(carBase, carOffsets.currentLap, readInteger) or 0,
        turnNumber = readMemorySafe(driverBase, driverOffsets.turnNumber, readInteger) or 0,
        speed = readMemorySafe(driverBase, driverOffsets.speed, readInteger) or 0,
        gear = readMemorySafe(driverBase, driverOffsets.gear, readInteger) or 0,
        rpm = readMemorySafe(driverBase, driverOffsets.rpm, readInteger) or 0,
        currentLapTime = readMemorySafe(driverBase, driverOffsets.currentLapTime, readFloat) or 0,
        tyreCompound = tyreCompoundMap[readMemorySafe(carBase, carOffsets.tyreCompound, readByte)] or "Unknown",
        paceMode = paceModeMap[readMemorySafe(carBase, carOffsets.paceMode, readByte) or 2] or "Unknown",
        fuelMode = fuelModeMap[readMemorySafe(carBase, carOffsets.fuelMode, readByte) or 1] or "Unknown",
        ersMode = ersModeMap[readMemorySafe(carBase, carOffsets.ersMode, readByte) or 0] or "Unknown",
        flTemp = readMemorySafe(carBase, carOffsets.flTemp, readFloat) or 0,
        flDeg = readMemorySafe(carBase, carOffsets.flDeg, readFloat) or 0,
        frTemp = readMemorySafe(carBase, carOffsets.frTemp, readFloat) or 0,
        frDeg = readMemorySafe(carBase, carOffsets.frDeg, readFloat) or 0,
        rlTemp = readMemorySafe(carBase, carOffsets.rlTemp, readFloat) or 0,
        rlDeg = readMemorySafe(carBase, carOffsets.rlDeg, readFloat) or 0,
        rrTemp = readMemorySafe(carBase, carOffsets.rrTemp, readFloat) or 0,
        rrDeg = readMemorySafe(carBase, carOffsets.rrDeg, readFloat) or 0,
        engineTemp = readMemorySafe(carBase, carOffsets.engineTemp, readFloat) or 0,
        engDeg = readMemorySafe(carBase, carOffsets.engDeg, readFloat) or 0,
        gearboxDeg = readMemorySafe(carBase, carOffsets.gearboxDeg, readFloat) or 0,
        ersDeg = readMemorySafe(carBase, carOffsets.ersDeg, readFloat) or 0,
        charge = readMemorySafe(carBase, carOffsets.charge, readFloat) or 0,
        fuel = readMemorySafe(carBase, carOffsets.fuel, readFloat) or 0,
        bestSessionTime = readMemorySafe(sessionBase, sessionOffsets.bestSessionTime, readFloat) or 0,
        driverBestLap = readMemorySafe(driverBase, driverOffsets.driverBestLap, readFloat) or 0,
        lastLapTime = readMemorySafe(driverBase, driverOffsets.lastLapTime, readFloat) or 0,
        lastS1Time = readMemorySafe(driverBase, driverOffsets.lastS1Time, readFloat),
        lastS2Time = readMemorySafe(driverBase, driverOffsets.lastS2Time, readFloat),
        lastS3Time = readMemorySafe(driverBase, driverOffsets.lastS3Time, readFloat),
        rubber = readMemorySafe(sessionBase, sessionOffsets.rubber, readFloat) or 0,
        airTemp = readMemorySafe(weatherBase, weatherOffsets.airTemp, readFloat) or 0,
        trackTemp = readMemorySafe(weatherBase, weatherOffsets.trackTemp, readFloat) or 0
    }
end

--#endregion
-- ==================================
-- TIMER
-- ==================================
--#region

local loggingTimer = createTimer(nil, false)
loggingTimer.Interval = 10

function sendData()
    local allData = {}  -- Master table to hold all cars' data
    
    for carName, _ in pairs(carscarOffsets) do
        local rawData = collectDriverData(carName)
        if rawData then
            -- Organize into hierarchical structure
            allData[carName] = {
                telemetry = {
                    car = {
                        driverNumber = rawData.driverNumber,
                        speed = rawData.speed,
                        rpm = rawData.rpm,
                        gear = rawData.gear,
                        charge = rawData.charge,
                        fuel = rawData.fuel,
                        lap = {
                            current = rawData.currentLap,
                            position = rawData.turnNumber,
                            time = {
                                current = rawData.currentLapTime,
                                last = rawData.lastLapTime,
                                best = rawData.driverBestLap,
                                sector = {
                                    last = {
                                        s1 = rawData.lastS1Time,
                                        s2 = rawData.lastS2Time,
                                        s3 = rawData.lastS3Time,
                                    }
                                }
                            }
                        },
                        tyres = {
                            compound = rawData.tyreCompound,
                            temps = {
                                front_left = rawData.flTemp,
                                front_right = rawData.frTemp,
                                rear_left = rawData.rlTemp,
                                rear_right = rawData.rrTemp,
                            },
                            wear = {
                                front_left = rawData.flDeg,
                                front_right = rawData.frDeg,
                                rear_left = rawData.rlDeg,
                                rear_right = rawData.rrDeg,
                            }
                        },
                        modes = {
                            pace = rawData.paceMode,
                            fuel = rawData.fuelMode,
                            ers = rawData.ersMode
                        },
                        components = {
                            engine = {
                                temp = rawData.engineTemp,
                                deg = rawData.engDeg,
                            },
                            ers = {
                                deg = rawData.ersDeg
                            },
                            gearbox = {
                                deg = rawData.gearboxDeg
                            }
                        }
                    },

                    session = {
                        time_elapsed = rawData.timeElasped,
                        best_time = rawData.bestSessionTime,
                        track = {
                            rubber = rawData.rubber,
                            temp = rawData.trackTemp
                        },
                        weather = {
                            air_temp = rawData.airTemp
                        }
                    }
                }
            }
        end
    end

    -- Write structured data to shared memory
    writeStructuredData(allData)
end

function writeStructuredData(data)
    if not mmf then return false end
    
    mmf:seek("set", 0)
    
    local function serializeValue(v)
        if type(v) == "number" then
            return string.format("%.15g", v)
        elseif type(v) == "string" then
            return string.format('"%s"', v:gsub('"', '\\"'))
        elseif type(v) == "table" then
            local parts = {}
            for k, v2 in pairs(v) do
                table.insert(parts, string.format('"%s":%s', k, serializeValue(v2)))
            end
            return "{" .. table.concat(parts, ",") .. "}"
        else
            return "null"
        end
    end
    
    local jsonStr = serializeValue(data)
    
    -- Write with length prefix
    mmf:write(string.pack("<I4", #jsonStr))
    mmf:write(jsonStr)
    mmf:flush()
    return true
end

loggingTimer.OnTimer = sendData

--#endregion
-- ==================================
-- INITIALIZATION
-- ==================================
--#region

local function initialize()
    local process = openProcess("F1Manager24.exe")
    if not process then
        return false
    end

    loggingTimer.Enabled = true
    return true
end

function OnClose()
    if mmf then
        mmf:close()
    end
end

initialize()

--#endregion