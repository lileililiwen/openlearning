/* SCORM 1.2 runtime API adapter.
 *
 * Exposed on window.API (SCORM 1.2 convention) in the launch page, which is the
 * parent of the SCO iframe, so SCOs can use window.parent.API.
 *
 * State is cached client-side and flushed to the server on Commit/Terminate via
 * synchronous XHR so the API surface stays synchronous (SCORM requires
 * Initialize/Terminate to return immediately).
 */
(function () {
    "use strict";

    var config = window.scormConfig || {};
    var packageId = config.packageId;

    var state = {
        lesson_location: "",
        suspend_data: "",
        lesson_status: "not attempted",
        score_raw: "",
        session_time: ""
    };

    var initialized = false;
    var lastError = "0";

    var errorMessages = {
        "0": "No error",
        "101": "General exception",
        "201": "Invalid argument error",
        "202": "Element cannot have value",
        "203": "Element is not initialized",
        "301": "Not initialized",
        "401": "Not implemented",
        "402": "Invalid set value, element is read-only",
        "403": "Element is read only",
        "404": "Element is write only",
        "405": "Incorrect data type"
    };

    // Elements the SCO may write through the data model.
    var writableElements = {
        "cmi.core.lesson_location": "lesson_location",
        "cmi.core.lesson_status": "lesson_status",
        "cmi.core.score.raw": "score_raw",
        "cmi.core.session_time": "session_time",
        "cmi.suspend_data": "suspend_data",
        "cmi.core.entry": null,
        "cmi.core.exit": null
    };

    function syncPost(path, body) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", path, false);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.send(JSON.stringify(body));
        if (xhr.status >= 200 && xhr.status < 300) {
            try {
                return JSON.parse(xhr.responseText);
            } catch (e) {
                return null;
            }
        }
        return null;
    }

    function flush() {
        if (!packageId) {
            return;
        }
        syncPost("/scorm/runtime/commit", {
            packageId: packageId,
            lessonLocation: state.lesson_location,
            suspendData: state.suspend_data,
            lessonStatus: state.lesson_status,
            scoreRaw: state.score_raw,
            sessionTime: state.session_time
        });
    }

    var API = {
        Initialize: function () {
            if (initialized) {
                lastError = "101";
                return "false";
            }
            initialized = true;
            lastError = "0";
            return "true";
        },

        Terminate: function () {
            if (!initialized) {
                lastError = "301";
                return "false";
            }
            flush();
            initialized = false;
            lastError = "0";
            return "true";
        },

        GetValue: function (element) {
            if (!initialized) {
                lastError = "301";
                return "";
            }

            var key = writableElements[element];
            if (element === "cmi.core.student_id") {
                lastError = "0";
                return config.studentId || "";
            }
            if (element === "cmi.core.student_name") {
                lastError = "0";
                return config.studentName || "";
            }
            if (element === "cmi.core.lesson_mode") {
                lastError = "0";
                return "normal";
            }
            if (element === "cmi.core.credit") {
                lastError = "0";
                return "credit";
            }
            if (element === "cmi.core.entry") {
                lastError = "0";
                return state.lesson_location || state.suspend_data ? "resume" : "ab-initio";
            }
            if (element === "cmi.core.total_time" || element === "cmi.core.session_time") {
                lastError = "0";
                return state.session_time;
            }
            if (key === undefined || key === null) {
                lastError = "401";
                return "";
            }

            lastError = "0";
            return state[key] || "";
        },

        SetValue: function (element, value) {
            if (!initialized) {
                lastError = "301";
                return "false";
            }
            var key = writableElements[element];
            if (key === undefined || key === null) {
                lastError = "402";
                return "false";
            }
            state[key] = String(value);
            lastError = "0";
            return "true";
        },

        Commit: function () {
            if (!initialized) {
                lastError = "301";
                return "false";
            }
            flush();
            lastError = "0";
            return "true";
        },

        GetLastError: function () {
            return lastError;
        },

        GetErrorString: function (errorCode) {
            return errorMessages[String(errorCode)] || "";
        },

        GetDiagnostic: function (errorCode) {
            return errorMessages[String(errorCode)] || "";
        },

        GetVersion: function () {
            return "1.0";
        }
    };

    // Preload persisted state synchronously so GetValue works right after Initialize.
    if (packageId) {
        var data = syncPost("/scorm/runtime/init", { packageId: packageId });
        if (data) {
            state.lesson_location = data.lessonLocation || "";
            state.suspend_data = data.suspendData || "";
            state.lesson_status = data.lessonStatus || "not attempted";
            state.score_raw = data.scoreRaw || "";
            state.session_time = data.sessionTime || "";
        }
    }

    window.API = API;
    window.API_1484_11 = API;
})();
