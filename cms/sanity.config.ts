import {defineConfig} from 'sanity'
import {structureTool} from 'sanity/structure'
import {exhibition} from './schemaTypes/exhibition'

const projectId = process.env.SANITY_STUDIO_PROJECT_ID
if (!projectId) throw new Error('Set SANITY_STUDIO_PROJECT_ID in cms/.env.local before starting Studio.')

export default defineConfig({
  name: 'museum-explorer',
  title: 'Museum Explorer',
  projectId,
  dataset: process.env.SANITY_STUDIO_DATASET || 'production',
  plugins: [structureTool()],
  schema: {types: [exhibition]},
})
