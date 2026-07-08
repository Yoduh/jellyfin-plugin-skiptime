"use strict";

var SkipTimePage = {
    selectedItem: null,

    segments: [],

    searchTimer: null,

    initialize: function () {
        const page = document.querySelector("#SkipTimeConfigPage");

        this.page = page;

        this.searchBox = page.querySelector("#txtSearch");

        this.searchResults = page.querySelector("#searchResults");

        this.selectedTitle = page.querySelector("#selectedTitle");

        this.segmentTable = page.querySelector("#segmentTableBody");

        this.btnAdd = page.querySelector("#btnAdd");

        this.btnDelete = page.querySelector("#btnDelete");

        this.btnSave = page.querySelector("#btnSave");

        this.wireEvents();
    },

    wireEvents: function () {
        this.searchBox.addEventListener("input", () => {
            clearTimeout(this.searchTimer);

            this.searchTimer = setTimeout(() => {
                this.search(this.searchBox.value);
            }, 250);
        });

        this.btnAdd.addEventListener("click", () => this.addSegment());

        this.btnDelete.addEventListener("click", () => this.deleteAll());

        this.btnSave.addEventListener("click", (e) => {
            e.preventDefault();

            this.save();
        });
    },

    api: async function (url, options) {
        return ApiClient.fetch(
            Object.assign(
                {
                    url: url,
                },
                options || {},
            ),
        );
    },

    search: async function (query) {
        query = query.trim();

        if (!query.length) {
            this.searchResults.innerHTML = "";

            return;
        }

        const items = await this.api("SkipTime/Search?query=" + encodeURIComponent(query));

        this.renderSearchResults(items);
    },

    renderSearchResults: function (items) {
        this.searchResults.innerHTML = "";

        for (const item of items) {
            const row = document.createElement("div");

            row.className = "listItem listItem-border";

            row.innerHTML = `
<div class="listItemBody">
<div class="listItemBodyText">
${this.escape(item.name)}
</div>
<div class="secondary">
${item.type}
${item.productionYear ?? ""}
</div>
</div>
`;

            row.addEventListener("click", () => this.selectItem(item));

            this.searchResults.appendChild(row);
        }
    },

    selectItem: async function (item) {
        this.selectedItem = item;

        this.selectedTitle.textContent = item.name + (item.productionYear ? " (" + item.productionYear + ")" : "");

        this.segments = await this.api("SkipTime/" + item.itemId);

        this.renderSegments();
    },
    renderSegments: function () {
        this.segmentTable.innerHTML = "";

        if (!this.segments.length) {
            const row = document.createElement("tr");

            row.innerHTML = `
<td colspan="3" class="secondaryText">
No skip ranges configured.
</td>
`;

            this.segmentTable.appendChild(row);

            return;
        }

        for (const segment of this.segments) {
            this.segmentTable.appendChild(this.createSegmentRow(segment));
        }
    },

    createSegmentRow: function (segment) {
        const row = document.createElement("tr");

        const startCell = document.createElement("td");
        const endCell = document.createElement("td");
        const actionCell = document.createElement("td");

        const startInput = document.createElement("input");

        startInput.type = "text";
        startInput.className = "emby-input";
        startInput.value = this.ticksToTime(segment.startTicks);

        const endInput = document.createElement("input");

        endInput.type = "text";
        endInput.className = "emby-input";
        endInput.value = this.ticksToTime(segment.endTicks);

        startInput.addEventListener("change", () => {
            segment.startTicks = this.timeToTicks(startInput.value);
        });

        endInput.addEventListener("change", () => {
            segment.endTicks = this.timeToTicks(endInput.value);
        });

        const deleteButton = document.createElement("button");

        deleteButton.className = "raised button-delete emby-button";

        deleteButton.type = "button";

        deleteButton.textContent = "Delete";

        deleteButton.addEventListener("click", () => {
            const index = this.segments.indexOf(segment);

            if (index >= 0) {
                this.segments.splice(index, 1);

                this.renderSegments();
            }
        });

        startCell.appendChild(startInput);
        endCell.appendChild(endInput);
        actionCell.appendChild(deleteButton);

        row.appendChild(startCell);
        row.appendChild(endCell);
        row.appendChild(actionCell);

        return row;
    },

    addSegment: function () {
        this.segments.push({
            startTicks: 0,

            endTicks: 10000000,
        });

        this.renderSegments();
    },

    validateSegments: function () {
        this.segments.sort(function (a, b) {
            return a.startTicks - b.startTicks;
        });

        for (let i = 0; i < this.segments.length; i++) {
            const current = this.segments[i];

            if (current.startTicks < 0) {
                throw new Error("Start time cannot be negative.");
            }

            if (current.endTicks <= current.startTicks) {
                throw new Error("End time must be after Start time.");
            }

            if (i === 0) {
                continue;
            }

            const previous = this.segments[i - 1];

            if (current.startTicks < previous.endTicks) {
                throw new Error("Skip ranges may not overlap.");
            }
        }
    },

    ticksToTime: function (ticks) {
        let totalSeconds = Math.floor(ticks / 10000000);

        const hours = Math.floor(totalSeconds / 3600);

        totalSeconds -= hours * 3600;

        const minutes = Math.floor(totalSeconds / 60);

        totalSeconds -= minutes * 60;

        const seconds = totalSeconds;

        return [hours, minutes, seconds]
            .map(function (x) {
                return x.toString().padStart(2, "0");
            })
            .join(":");
    },

    timeToTicks: function (value) {
        const parts = value.trim().split(":");

        if (parts.length !== 3) {
            throw new Error("Time must be HH:MM:SS");
        }

        const hours = parseInt(parts[0], 10);

        const minutes = parseInt(parts[1], 10);

        const seconds = parseInt(parts[2], 10);

        if (Number.isNaN(hours) || Number.isNaN(minutes) || Number.isNaN(seconds)) {
            throw new Error("Invalid time.");
        }

        return (hours * 3600 + minutes * 60 + seconds) * 10000000;
    },
    save: async function () {
        if (!this.selectedItem) {
            Dashboard.alert("Please select a movie or episode.");

            return;
        }

        try {
            this.validateSegments();

            await this.api("SkipTime", {
                type: "POST",

                contentType: "application/json",

                data: JSON.stringify({
                    itemId: this.selectedItem.itemId,

                    segments: this.segments,
                }),
            });

            Dashboard.toast({
                text: "Skip ranges saved.",
            });
        } catch (e) {
            Dashboard.alert(e.message || e);
        }
    },

    deleteAll: async function () {
        if (!this.selectedItem) {
            Dashboard.alert("Please select a movie or episode.");

            return;
        }

        const confirmed = await Dashboard.confirm({
            title: "Delete Skip Ranges",

            text: 'Delete every skip range for "' + this.selectedItem.name + '"?',
        });

        if (!confirmed) {
            return;
        }

        await this.api("SkipTime/" + this.selectedItem.itemId, {
            type: "DELETE",
        });

        this.segments = [];

        this.renderSegments();

        Dashboard.toast({
            text: "Skip ranges deleted.",
        });
    },

    escape: function (text) {
        if (text === null || text === undefined) {
            return "";
        }

        return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    },
};

document.addEventListener("DOMContentLoaded", function () {
    SkipTimePage.initialize();
});
