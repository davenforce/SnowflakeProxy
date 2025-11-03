// Chart.js Interop for SnowflakeProxy
window.ChartJsInterop = {
    charts: {},

    /**
     * Initialize a Chart.js chart
     * @param {string} canvasId - The canvas element ID
     * @param {object} config - Chart.js configuration object
     * @returns {boolean} - Success status
     */
    initialize: function (canvasId, config) {
        try {
            // Destroy existing chart if present
            if (this.charts[canvasId]) {
                this.charts[canvasId].destroy();
            }

            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.error(`Canvas element with id '${canvasId}' not found`);
                return false;
            }

            const ctx = canvas.getContext('2d');
            this.charts[canvasId] = new Chart(ctx, config);

            return true;
        } catch (error) {
            console.error('Error initializing Chart.js:', error);
            return false;
        }
    },

    /**
     * Update chart data
     * @param {string} canvasId - The canvas element ID
     * @param {object} newData - New data object
     * @returns {boolean} - Success status
     */
    update: function (canvasId, newData) {
        try {
            const chart = this.charts[canvasId];
            if (!chart) {
                console.error(`Chart with id '${canvasId}' not found`);
                return false;
            }

            chart.data = newData;
            chart.update();
            return true;
        } catch (error) {
            console.error('Error updating Chart.js:', error);
            return false;
        }
    },

    /**
     * Destroy a chart
     * @param {string} canvasId - The canvas element ID
     * @returns {boolean} - Success status
     */
    destroy: function (canvasId) {
        try {
            const chart = this.charts[canvasId];
            if (chart) {
                chart.destroy();
                delete this.charts[canvasId];
            }
            return true;
        } catch (error) {
            console.error('Error destroying Chart.js:', error);
            return false;
        }
    }
};
