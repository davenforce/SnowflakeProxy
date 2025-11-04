/**
 * Vega-Lite JSInterop for secure chart rendering
 * Prevents XSS by using DOM APIs instead of innerHTML
 */
window.VegaLiteInterop = {
    /**
     * Renders a Vega-Lite chart in the specified container
     * @param {string} containerId - The ID of the container element
     * @param {string} specJson - JSON string of the Vega-Lite specification
     * @returns {Promise<void>}
     */
    render: async function (containerId, specJson) {
        try {
            if (typeof vegaEmbed === 'undefined') {
                console.error('Vega-Lite not loaded. Please include vega-embed script.');
                return;
            }

            const spec = JSON.parse(specJson);
            const container = document.getElementById(containerId);

            if (!container) {
                console.error(`Container ${containerId} not found`);
                return;
            }

            await vegaEmbed(`#${containerId}`, spec, {
                actions: false,
                renderer: 'svg'
            });
        } catch (error) {
            console.error('Error rendering Vega-Lite chart:', error);
            throw error;
        }
    },

    /**
     * Destroys a Vega-Lite chart instance
     * @param {string} containerId - The ID of the container element
     */
    destroy: function (containerId) {
        try {
            const container = document.getElementById(containerId);
            if (container) {
                container.innerHTML = '';
            }
        } catch (error) {
            console.error('Error destroying Vega-Lite chart:', error);
        }
    }
};
