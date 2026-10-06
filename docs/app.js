// -----------------------------------------------------------------------------
// CleanImp benchmark dashboard
// -----------------------------------------------------------------------------
// This file:
//   1. Loads benchmark results from data.json.
//   2. Updates filters dynamically.
//   3. Builds the algorithm checkboxes.
//   4. Draws the Plotly chart.
//   5. Displays the numerical values in a table.
//
// The Plotly legend is dynamic and centered at the TOP of the chart.
// Only currently selected algorithms appear in the legend.
// -----------------------------------------------------------------------------

// Small helper for accessing HTML elements by ID.
const $ = (id) => document.getElementById(id);

let DB;


// -----------------------------------------------------------------------------
// General helpers
// -----------------------------------------------------------------------------

// Return unique, non-empty values sorted alphabetically.
const uniq = (values) =>
    [...new Set(
        values.filter(
            (value) =>
                value !== null &&
                value !== undefined &&
                value !== ""
        )
    )].sort((a, b) => String(a).localeCompare(String(b)));


// Is the dashboard currently displaying an upstream experiment?
const isUpstream = () =>
    $("experiment").value === "Upstream";


// Is the downstream classification view active?
const classifierActive = () =>
    $("experiment").value === "Downstream" &&
    $("task").value === "Classification";


// Is the downstream forecasting view active?
const forecasterActive = () =>
    $("experiment").value === "Downstream" &&
    $("task").value === "Forecasting";


// -----------------------------------------------------------------------------
// Dropdown helpers
// -----------------------------------------------------------------------------

/**
 * Replace the options of a <select>.
 *
 * If preserve=true, the current selection is kept whenever it still exists.
 */
function setOptions(id, values, preserve = true) {
    const element = $(id);
    const oldValue = element.value;

    element.innerHTML = values
        .map((value) => `<option value="${value}">${value}</option>`)
        .join("");

    element.value =
        preserve && values.includes(oldValue)
            ? oldValue
            : (values[0] || "");
}


/**
 * Replace the options of a <select> while giving one value priority.
 *
 * Example:
 *   - RMSE for metrics
 *   - arsenal for classifiers
 *   - chronos for forecasters
 *   - mcar for missingness patterns
 */
function setPreferred(id, values, preferred) {
    const element = $(id);
    const oldValue = element.value;

    element.innerHTML = values
        .map((value) => `<option value="${value}">${value}</option>`)
        .join("");

    if (values.includes(oldValue)) {
        element.value = oldValue;
    } else if (values.includes(preferred)) {
        element.value = preferred;
    } else {
        element.value = values[0] || "";
    }
}


// -----------------------------------------------------------------------------
// Rebuild filters
// -----------------------------------------------------------------------------

/**
 * Rebuild all dependent filters whenever the user changes a selection.
 *
 * Each filtering step reduces the available rows before constructing the next
 * dropdown. This means users only see combinations that actually exist in
 * data.json.
 */
function rebuild() {

    // Experiment
    setOptions(
        "experiment",
        uniq(DB.results.map((row) => row.experiment)),
        true
    );

    let rows = DB.results.filter(
        (row) => row.experiment === $("experiment").value
    );


    // Task
    setOptions(
        "task",
        uniq(rows.map((row) => row.task)),
        true
    );

    rows = rows.filter(
        (row) => row.task === $("task").value
    );


    // Metric
    //
    // Default / first metric depends on the current experiment and task:
    //   Upstream                  -> RMSE
    //   Downstream Classification -> F1
    //   Downstream Forecasting    -> SMAPE
    //
    // The preferred metric is explicitly moved to the first position so the
    // dropdown order matches the default selection.
    const preferredMetric =
        $("experiment").value === "Upstream"
            ? "RMSE"
            : $("task").value === "Classification"
                ? "F1"
                : "SMAPE";

    const availableMetrics = uniq(
        rows.map((row) => row.metric)
    );

    const orderedMetrics = [
        preferredMetric,
        ...availableMetrics.filter(
            (metric) => metric !== preferredMetric
        )
    ].filter(
        (metric) => availableMetrics.includes(metric)
    );

    setPreferred(
        "metric",
        orderedMetrics,
        preferredMetric
    );

    rows = rows.filter(
        (row) => row.metric === $("metric").value
    );


    // Show the classifier selector only for Downstream / Classification.
    $("classifier-wrap").classList.toggle(
        "hidden",
        !classifierActive()
    );

    // Show the forecaster selector only for Downstream / Forecasting.
    $("forecaster-wrap").classList.toggle(
        "hidden",
        !forecasterActive()
    );


    // Classifier
    if (classifierActive()) {
        setPreferred(
            "classifier",
            uniq(rows.map((row) => row.classifier)),
            "arsenal"
        );

        rows = rows.filter(
            (row) => row.classifier === $("classifier").value
        );
    } else {
        $("classifier").innerHTML = "";
    }


    // Forecaster
    //
    // Because the available forecasters are derived AFTER filtering by metric,
    // a metric that only contains Chronos results will automatically expose
    // Chronos as the only possible forecaster.
    if (forecasterActive()) {
        setPreferred(
            "forecaster",
            uniq(rows.map((row) => row.forecaster)),
            "chronos"
        );

        rows = rows.filter(
            (row) => row.forecaster === $("forecaster").value
        );
    } else {
        $("forecaster").innerHTML = "";
    }


    // Missingness pattern
    setPreferred(
        "pattern",
        uniq(rows.map((row) => row.pattern)),
        "mcar"
    );

    rows = rows.filter(
        (row) => row.pattern === $("pattern").value
    );


    // Dataset
    setOptions(
        "dataset",
        uniq(rows.map((row) => row.dataset)),
        true
    );

    rows = rows.filter(
        (row) => row.dataset === $("dataset").value
    );


    // Upstream experiments use algorithm families.
    // Downstream experiments display the available imputers directly.
    $("family-wrap").classList.toggle(
        "hidden",
        !isUpstream()
    );

    if (isUpstream()) {
        const families = uniq(
            rows
                .map((row) => row.family)
                .filter((family) => family !== "Baseline")
        );

        setOptions("family", families, true);

        // MeanImpute is always included as the baseline.
        buildAlgos(
            rows.filter(
                (row) =>
                    row.family === $("family").value ||
                    row.algo === "MeanImpute"
            )
        );
    } else {
        $("family").innerHTML = "";
        buildAlgos(rows);
    }

    render();
}


// -----------------------------------------------------------------------------
// Algorithm checkboxes
// -----------------------------------------------------------------------------

function buildAlgos(rows) {

    // Remember algorithms that were already selected.
    const oldSelection = new Set(
        [...$("algos").querySelectorAll("input:checked")]
            .map((input) => input.value)
    );

    const algorithms = uniq(
        rows.map((row) => row.algo)
    );

    // Keep MeanImpute first because it is the baseline.
    algorithms.sort((a, b) => {
        if (a === "MeanImpute") return -1;
        if (b === "MeanImpute") return 1;

        return String(a).localeCompare(String(b));
    });

    $("algos").innerHTML = algorithms
        .map(
            (algorithm) =>
                `<label>
                    <input
                        type="checkbox"
                        value="${algorithm}"
                        ${!oldSelection.size || oldSelection.has(algorithm)
                            ? "checked"
                            : ""}
                    >
                    ${algorithm}
                </label>`
        )
        .join("");

    // Redraw immediately whenever an algorithm is checked/unchecked.
    $("algos")
        .querySelectorAll("input")
        .forEach((input) => {
            input.onchange = render;
        });
}


// -----------------------------------------------------------------------------
// Data selection
// -----------------------------------------------------------------------------

/**
 * Return only rows corresponding to the current dashboard configuration.
 */
function rowsForView() {
    let rows = DB.results.filter(
        (row) =>
            row.experiment === $("experiment").value &&
            row.task === $("task").value &&
            row.metric === $("metric").value &&
            row.pattern === $("pattern").value &&
            row.dataset === $("dataset").value
    );

    if (classifierActive()) {
        rows = rows.filter(
            (row) => row.classifier === $("classifier").value
        );
    }

    if (forecasterActive()) {
        rows = rows.filter(
            (row) => row.forecaster === $("forecaster").value
        );
    }

    if (isUpstream()) {
        rows = rows.filter(
            (row) =>
                row.family === $("family").value ||
                row.algo === "MeanImpute"
        );
    }

    return rows;
}


/**
 * Group rows by selected algorithm and order every series by missing rate.
 */
function grouped(rows) {
    const selectedAlgorithms = new Set(
        [...$("algos").querySelectorAll("input:checked")]
            .map((input) => input.value)
    );

    const groups = new Map();

    rows
        .filter((row) => selectedAlgorithms.has(row.algo))
        .forEach((row) => {
            if (!groups.has(row.algo)) {
                groups.set(row.algo, []);
            }

            groups.get(row.algo).push(row);
        });

    return [...groups.entries()].map(
        ([algorithm, algorithmRows]) => ({
            algo: algorithm,
            rs: algorithmRows.sort(
                (a, b) => a.rate - b.rate
            )
        })
    );
}


// -----------------------------------------------------------------------------
// Values table
// -----------------------------------------------------------------------------

const fmt = (value) =>
    value == null
        ? "—"
        : Number(value).toFixed(4);


function renderTable(series) {
    const rates = uniq(
        series.flatMap(
            (item) => item.rs.map((row) => row.rate)
        )
    )
        .map(Number)
        .sort((a, b) => a - b);

    const valueAtRate = (item, rate) =>
        item.rs.find((row) => row.rate === rate)?.value;

    $("values-table").innerHTML = `
        <thead>
            <tr>
                <th>Algorithm</th>
                ${rates
                    .map((rate) => `<th>${rate}</th>`)
                    .join("")}
            </tr>
        </thead>

        <tbody>
            ${series
                .map(
                    (item) => `
                        <tr>
                            <td>${item.algo}</td>

                            ${rates
                                .map((rate) => {
                                    const value =
                                        valueAtRate(item, rate);

                                    return `
                                        <td class="${value == null
                                            ? "missing"
                                            : ""}">
                                            ${fmt(value)}
                                        </td>
                                    `;
                                })
                                .join("")}
                        </tr>
                    `
                )
                .join("")}
        </tbody>
    `;

    $("tablemeta").textContent =
        `${series.length} selected algorithm` +
        `${series.length === 1 ? "" : "s"} · ` +
        `${$("metric").value}`;
}


// -----------------------------------------------------------------------------
// Plot
// -----------------------------------------------------------------------------

function render() {
    const series = grouped(rowsForView());
    const metric = $("metric").value;


    // One Plotly trace per selected algorithm.
    //
    // The trace "name" is what Plotly displays in the dynamic legend.
    const traces = series.map((item) => ({
        x: item.rs.map((row) => row.rate),
        y: item.rs.map((row) => row.value),

        name: item.algo,

        type: "scatter",
        mode: "lines+markers",

        // Missing values remain visible as gaps instead of being connected.
        connectgaps: false,

        hovertemplate:
            `${item.algo}` +
            `<br>Rate: %{x}` +
            `<br>${metric}: %{y:.4f}` +
            `<extra></extra>`
    }));


    // Plotly layout.
    const layout = {

        // Extra room at the top for the centered dynamic legend.
        margin: {
            l: 72,
            r: 28,
            t: 90,
            b: 58
        },

        xaxis: {
            title: {
                text: "Missing rate",
                font: { size: 16 }
            },
            tickfont: { size: 13 }
        },

        yaxis: {
            title: {
                text: metric,
                font: { size: 16 }
            },
            tickfont: { size: 13 }
        },


        // -----------------------------------------------------------------
        // Dynamic series caption / legend
        // -----------------------------------------------------------------
        //
        // Plotly automatically shows only the traces currently present in
        // `traces`, so this legend follows the user's algorithm selection.
        //
        // x = 0.5 + xanchor = "center"  -> horizontally centered
        // y = 1.08 + yanchor = "bottom" -> placed just above the plot area
        // orientation = "h"             -> algorithms displayed in one row
        //
        legend: {
            orientation: "h",

            x: 0.5,
            xanchor: "center",

            y: 1.08,
            yanchor: "bottom",

            font: {
                size: 14
            },

            itemsizing: "constant"
        },

        hovermode: "closest",

        paper_bgcolor: "#fff",
        plot_bgcolor: "#fff"
    };


    const config = {
        responsive: true,
        displaylogo: false
    };


    Plotly.react(
        "plot",
        traces,
        layout,
        config
    );


    // Chart title.
    $("title").textContent =
        `${$("dataset").value} · ${$("pattern").value}`;


    // Additional information shown in the chart header.
    const extra =
        classifierActive()
            ? ` · ${$("classifier").value}`
            : forecasterActive()
                ? ` · ${$("forecaster").value}`
                : isUpstream()
                    ? ` · ${$("family").value}`
                    : "";


    $("meta").textContent =
        `${series.length} selected algorithm` +
        `${series.length === 1 ? "" : "s"} · ` +
        `${$("experiment").value} / ${$("task").value}` +
        `${extra} · ${metric}`;


    renderTable(series);
}


// -----------------------------------------------------------------------------
// Application startup
// -----------------------------------------------------------------------------

async function start() {

    // Load benchmark results.
    DB = await fetch("data.json")
        .then((response) => response.json());


    // Rebuild dependent filters whenever one of these controls changes.
    [
        "experiment",
        "task",
        "metric",
        "classifier",
        "forecaster",
        "pattern",
        "dataset",
        "family"
    ].forEach((id) => {
        $(id).onchange = rebuild;
    });


    // Initialize the dashboard.
    setOptions(
        "experiment",
        uniq(DB.results.map((row) => row.experiment)),
        false
    );

    rebuild();


    // Select all algorithms.
    $("all").onclick = () => {
        $("algos")
            .querySelectorAll("input")
            .forEach((input) => {
                input.checked = true;
            });

        render();
    };


    // Clear all algorithms.
    $("none").onclick = () => {
        $("algos")
            .querySelectorAll("input")
            .forEach((input) => {
                input.checked = false;
            });

        render();
    };
}


// Start the dashboard.
start();
