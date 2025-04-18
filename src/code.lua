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
local SHARED_MEM_SIZE = 1024*1024  -- 1MB buffer (adjust as needed)
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

pitstopStatusMap = {
    [0]="None",[1]="Requested",[2]="Entering",[3]="Queuing",[4]="Stopped",[5]="Exiting",[6]="In Garage",[7]="Jack Up",[8]="Releasing",[9]="Car Setup",[10]="Pit Stop Approach",[11]="Pit Stop Penalty",[12]="Waiting for Release"
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

local drsMap = {
    [0]="Disabled",[1]="Detected",[2]="Enabled",[3]="Active"
}

local weatherMap = {
    [0]="None",[1]="Sunny",[2]="Partly Sunny",[4]="Cloudy",[8]="Light Rain",[16]="Moderate Rain",[32]="Heavy Rain"
}

local trackNameMap = {
    [0] = "INVALID",
    [1] = "Albert Park",
    [2] = "Bahrain",
    [3] = "Shanghai",
    [4] = "Baku",
    [5] = "Barcelona",
    [6] = "Monaco",
    [7] = "Montreal",
    [8] = "PaulRicard",
    [9] = "RedBull Ring",
    [10] = "Silverstone",
    [11] = "Jeddah",
    [12] = "Hungaroring",
    [13] = "Spa-Francorchamps",
    [14] = "Monza",
    [15] = "Marina Bay",
    [16] = "Sochi",
    [17] = "Suzuka",
    [18] = "Hermanos Rodriguez",
    [19] = "Circuit Of The Americas",
    [20] = "Interlagos",
    [21] = "Yas Marina",
    [22] = "Miami",
    [23] = "Zandvoort",
    [24] = "Imola",
    [25] = "Vegas",
    [26] = "Qatar"
}

local sessionTypeMap = {
    [0] = "Practice 1",
    [1] = "Practice 2",
    [2] = "Practice 3",
    [3] = "Qualiyfing 1",
    [4] = "Qualifying 2",
    [5] = "Qualifying 3",
    [6] = "Race",
    [7] = "Sprint",
    [8] = "Sprint Qualifying 1",
    [9] = "Sprint Qualifying 2",
    [10] = "Sprint Qualifying 3"
}

local sessionTypeShortMap = {
    [0] = "P1",
    [1] = "P2",
    [2] = "P3",
    [3] = "Q1",
    [4] = "Q2",
    [5] = "Q3",
    [6] = "R",
    [7] = "S",
    [8] = "SQ1",
    [9] = "SQ2",
    [10] = "SQ3"
}

--#endregion
-- CAR STRUCTURE OFFSETS
local dataStructure = {
    session = {
        timeElapsed = {source = "session", offset = 0x148, type = "float", default = 0}, --DONE
        rubber = {source = "session", offset = 0x278, type = "float", default = 0}, --DONE
        bestSessionTime = {source = "session", offset = 0x768, type = "float", default = 0}, --DONE
        trackID = {source = "session", offset = 0x228, type = "byte", enum = "trackName", default = "Unknown"}, --DONE
        sessionType = {source = "session", offset = 0x288, type = "byte", enum = "sessionType", default = "Unknown"}, --DONE
        sessionTypeShort = {source = "session", offset = 0x288, type = "byte", enum = "sessionTypeShort", default = "Unknown"} --DONE
    },
    driver = {
        driverNumber = {source = "driver", offset = 0x58C, type = "byte", default = 0}, --DONE
        turnNumber = {source = "driver", offset = 0x530, type = "integer", default = 0}, --DONE
        speed = {source = "driver", offset = 0x4F0, type = "integer", default = 0}, --DONE
        rpm = {source = "driver", offset = 0x4EC, type = "integer", default = 0}, --DONE
        gear = {source = "driver", offset = 0x524, type = "integer", default = 0}, --DONE
        positon = {source = "driver", offset = 0x528, type = "integer", default = 0}, --DONE
        drsMode = {source = "driver", offset = 0x521, type = "byte", enum = "drs", default = "Unknown"}, --DONE
        driverBestLap = {source = "driver", offset = 0x538, type = "float", default = 0}, --DONE
        lastLapTime = {source = "driver", offset = 0x540, type = "float", default = 0}, --DONE
        currentLapTime = {source = "driver", offset = 0x544, type = "float", default = 0}, --DONE
        lastS1Time = {source = "driver", offset = 0x548, type = "float", default = nil}, --DONE
        lastS2Time = {source = "driver", offset = 0x550, type = "float", default = nil}, --DONE
        lastS3Time = {source = "driver", offset = 0x558, type = "float", default = nil} --DONE
    },
    car = {
        currentLap = {source = "car", offset = 0x7E4, type = "integer", default = 0}, --DONE
        tyreCompound = {source = "car", offset = 0xED5, type = "byte", enum = "tyreCompound", default = "Unknown"}, --DONE
        pitstopStatus = {source = "car", offset = 0x8A8, type = "byte", enum = "pitstopStatus", default = "Unknown"}, --DONE
        paceMode = {source = "car", offset = 0xEF1, type = "byte", enum = "paceMode", default = "Unknown"}, --DONE
        fuelMode = {source = "car", offset = 0xEF0, type = "byte", enum = "fuelMode", default = "Unknown"}, --DONE
        ersMode = {source = "car", offset = 0xEF2, type = "byte", enum = "ersMode", default = "Unknown"}, --DONE
        flTemp = {source = "car", offset = 0x980, type = "float", default = 0}, --DONE
        flDeg = {source = "car", offset = 0x984, type = "float", default = 0}, --DONE
        frTemp = {source = "car", offset = 0x98C, type = "float", default = 0}, --DONE
        frDeg = {source = "car", offset = 0x990, type = "float", default = 0}, --DONE
        rlTemp = {source = "car", offset = 0x998, type = "float", default = 0}, --DONE
        rlDeg = {source = "car", offset = 0x99C, type = "float", default = 0}, --DONE
        rrTemp = {source = "car", offset = 0x9A4, type = "float", default = 0}, --DONE
        rrDeg = {source = "car", offset = 0x9A8, type = "float", default = 0}, --DONE
        engineTemp = {source = "car", offset = 0x77C, type = "float", default = 0}, --DONE
        engDeg = {source = "car", offset = 0x784, type = "float", default = 0}, --DONE
        gearboxDeg = {source = "car", offset = 0x78C, type = "float", default = 0}, --DONE
        ersDeg = {source = "car", offset = 0x788, type = "float", default = 0}, --DONE
        charge = {source = "car", offset = 0x878, type = "float", default = 0}, --DONE
        fuel = {source = "car", offset = 0x778, type = "float", default = 0} --DONE
    },
    weather = {
        airTemp = {source = "weather", offset = 0xAC, type = "float", default = 0}, --DONE
        trackTemp = {source = "weather", offset = 0xB0, type = "float", default = 0}, --DONE
        weather = {source = "weather", offset = 0xBC, type = "byte", enum = "weather", default = "Unknown"} --DONE
    },
    
    -- Pointer definitions
    pointers = {
        pilotDataptr = {source = "car", offset = 0x708, type = "pointer"},
        sessionDataptr = {source = "driver", offset = 0x940, type = "pointer"},
        weatherDataptr = {source = "session", offset = 0xA12990, type = "pointer"}
    }
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

local readFunctions = {
    byte = readByte,
    integer = readInteger,
    float = readFloat,
    pointer = readPointer8
}

local enumMaps = {
    pitstopStatus = pitstopStatusMap,
    paceMode = paceModeMap,
    fuelMode = fuelModeMap,
    ersMode = ersModeMap,
    drs = drsMap,
    weather = weatherMap,
    tyreCompound = tyreCompoundMap,
    trackName = trackNameMap,
    sessionType = sessionTypeMap,
    sessionTypeShort = sessionTypeShortMap
}

function collectDriverData(car)
    local carsBase = getAddress(fullPointerPath)
    if not carsBase or carsBase == 0 then
        sleep(50)  -- Add a delay to reduce CPU usage
        return nil
    end
    
    local carBase = carsBase + carscarOffsets[car]
    if not carBase or carBase == 0 then return nil end

    local bases = {
        car = carBase,
        driver = readMemoryAuto(carBase, dataStructure.pointers.pilotDataptr),
        session = nil,
        weather = nil
    }
    
    if bases.driver then
        bases.session = readMemoryAuto(bases.driver, dataStructure.pointers.sessionDataptr)
        if bases.session then
            bases.weather = readMemoryAuto(bases.session, dataStructure.pointers.weatherDataptr)
        end
    end

    local result = {}
    
    for category, fields in pairs(dataStructure) do
        if category ~= "pointers" then -- Skip the pointers section
            result[category] = {}
            for fieldName, fieldDef in pairs(fields) do
                if fieldDef.source and bases[fieldDef.source] then
                    local value = readMemoryAuto(bases[fieldDef.source], fieldDef)
                    result[category][fieldName] = value or fieldDef.default
                end
            end
        end
    end
    
    return result
end

function readMemoryAuto(baseAddress, fieldDef)
    if not baseAddress or baseAddress == 0 then return nil end
    
    local readFunc = readFunctions[fieldDef.type]
    if not readFunc then return nil end
    
    local rawValue = readMemorySafe(baseAddress, fieldDef.offset, readFunc)
    if not rawValue then return nil end
    
    if fieldDef.enum then
        local enumMap = enumMaps[fieldDef.enum]
        return enumMap and enumMap[rawValue] or nil
    end
    
    return rawValue
end

--#endregion
-- ==================================
-- TIMER
-- ==================================
--#region

local loggingTimer = createTimer(nil, false)
loggingTimer.Interval = 50

function sendData()
    local allData = {}  -- Master table to hold all cars' data
    
    for carName, _ in pairs(carscarOffsets) do
        local rawData = collectDriverData(carName)
        if rawData then
            -- Organize into hierarchical structure
            -- In sendData() function, modify the weather structure:
            allData[carName] = {
                telemetry = {
                    session = {
                        timeElapsed = rawData.session.timeElapsed or 0,
                        trackName = rawData.session.trackID or "Unknown",
                        bestSessionTime = rawData.session.bestSessionTime or 0,
                        rubber = rawData.session.rubber or 0,
                        sessionType = rawData.session.sessionType or "Unknown",
                        sessionTypeShort = rawData.session.sessionTypeShort or "Unknown",
                        weather = {  -- This should match Python structure
                            airTemp = rawData.weather.airTemp or 0,
                            trackTemp = rawData.weather.trackTemp or 0,
                            weather = rawData.weather.weather or "Unknown"
                        }
                    },
                    driver = {
                        position = rawData.driver.positon or 0,
                        driverNumber = rawData.driver.driverNumber or 0,
                        pitstopStatus = rawData.car.pitstopStatus or "Unknown",
                        status = {
                            turnNumber = rawData.driver.turnNumber or 0,
                            currentLap = rawData.car.currentLap or 0
                        },
                        timings = {
                            currentLapTime = rawData.driver.currentLapTime or 0,
                            driverBestLap = rawData.driver.driverBestLap or 0,
                            lastLapTime = rawData.driver.lastLapTime or 0,
                            sectors = {
                                lastS1Time = rawData.driver.lastS1Time or 0,
                                lastS2Time = rawData.driver.lastS2Time or 0,
                                lastS3Time = rawData.driver.lastS3Time or 0
                            }
                        },
                        car = {
                            speed = rawData.driver.speed or 0,
                            rpm = rawData.driver.rpm or 0,
                            gear = rawData.driver.gear or 0,
                            charge = rawData.car.charge or 0,
                            fuel = rawData.car.fuel or 0,
                            tyres = {
                                compound = rawData.car.tyreCompound or "Unknown",
                                temperature = {
                                    flTemp = rawData.car.flTemp or 0,
                                    frTemp = rawData.car.frTemp or 0,
                                    rlTemp = rawData.car.rlTemp or 0,
                                    rrTemp = rawData.car.rrTemp or 0
                                },
                                wear = {
                                flDeg = rawData.car.flDeg or 0,
                                    frDeg = rawData.car.frDeg or 0,
                                    rlDeg = rawData.car.rlDeg or 0,
                                    rrDeg = rawData.car.rrDeg or 0
                                }
                            },
                            modes = {
                                paceMode = rawData.car.paceMode or "Unknown",
                                fuelMode = rawData.car.fuelMode or "Unknown",
                                ersMode = rawData.car.ersMode or "Unknown",
                                drsMode = rawData.driver.drsMode or "Unknown"
                            },
                            components = {
                                engine = {
                                    engineTemp = rawData.car.engineTemp or 0,
                                    engineDeg = rawData.car.engDeg or 0
                                },
                                gearbox = {
                                    gearboxDeg = rawData.car.gearboxDeg or 0
                                },
                                ers = {
                                    ersDeg = rawData.car.ersDeg or 0
                                }
                            }
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